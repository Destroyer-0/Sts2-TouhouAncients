using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 雾雨魔理沙：偷牌的魔法使。
/// 状态机：逃逸速度 → 蘑菇分支（蘑菇少于上限时召唤，否则进入随机分支）→ 极限火花·蓄力 → 极限火花 → 循环。
/// </summary>
public sealed class KirisameMarisaMonster : TouhouAncientMonsterBase
{
    // --- HP ---
    /// <summary>
    /// 初始生命：二层数值（角色最早出现的幕，也作为图鉴预览等环境的回退值）。
    /// 三层时在 <see cref="AfterAddedToRoom"/> 中提升，因为 Creature 构造函数读取
    /// MinInitialHp/MaxInitialHp 时 Creature 尚未绑定，无法获取幕号。
    /// </summary>
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 156, 150);

    /// <summary>三层初始生命（当前数值）。</summary>
    private int InitialHpAct3 => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 256, 250);

    // --- 伤害/数值 ---
    /// <summary>
    /// 逃逸速度基础伤害：默认使用第二幕数值（15/14），第三幕额外配置为 22/20（A17+ 升阶 / 普通）。
    /// 图鉴预览等无法获取幕号的环境同样回退到第二幕数值。
    /// </summary>
    private int EscapeVelocityDamage => GetActValue(
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 14),
        (3, AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 22, 20)));

    private int StellarFantasyDamage => GetActValue(
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4),
        (3, AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5)));

    private int StellarFantasyHits => 4;

    private int BlackHoleEdgeDamage => GetActValue(
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15),
        (3, AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16)));

    private int BlackHoleEdgeWeak => 2;

    private int MasterSparkChargeBlock => GetActValue(14, (3, 20));

    private int MasterSparkChargeVigorPerMushroom => GetActValue(10, (3, 12));

    private int MasterSparkDamage => 20;

    private int MasterSparkStrength => 2;

    private const int MaxMushroomCount = 5;

    private const int FungusExpertSummonCount = 2;
    
    private static readonly LocString _mushroomLine =
        new LocString("monsters", "TOUHOUANCIENTS-KIRISAME_MARISA_MONSTER.moves.FUNGUS_EXPERT.banter");
    
    private static readonly LocString _prepareLine =
        new LocString("monsters", "TOUHOUANCIENTS-KIRISAME_MARISA_MONSTER.moves.MASTER_SPARK_CHARGE.banter");
    
    private static readonly LocString _thiefLine =
        new LocString("monsters", "TOUHOUANCIENTS-KIRISAME_MARISA_MONSTER.moves.ESCAPE_VELOCITY.banter");

    /// <summary>
    /// 魔理沙身后当前显示的偷取卡牌节点（仅本机创建），归还卡牌时统一清除。
    /// </summary>
    private readonly List<NCard> _stolenCardNodes = new();

    /// <summary>
    /// 场上存活蘑菇的数量。
    /// </summary>
    private int AliveMushroomCount => base.CombatState.Enemies.Count(c => c is { Monster: FantasyMushroomMonster, IsDead: false });

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (CurrentActNumber == 3 && InitialHpAct3 > InitialHp)
        {
            base.Creature.SetMaxHpInternal(InitialHpAct3);
            base.Creature.SetCurrentHpInternal(InitialHpAct3);
        }
        // 施加量 = 每蘑菇提供的活力值（Counter 类型，层数即活力值）
        await PowerCmd.Apply<MagicianPower>(new ThrowingPlayerChoiceContext(), base.Creature, MasterSparkChargeVigorPerMushroom, base.Creature, null);
    }

    // --- 状态机 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();

        MoveState escapeVelocity = new MoveState("ESCAPE_VELOCITY", EscapeVelocityMove,
            new SingleAttackIntent(EscapeVelocityDamage), new CardDebuffIntent());
        MoveState fungusExpert = new MoveState("FUNGUS_EXPERT", FungusExpertMove, new SummonIntent());
        MoveState stellarFantasy = new MoveState("STELLAR_FANTASY", StellarFantasyMove,
            new MultiAttackIntent(StellarFantasyDamage, StellarFantasyHits));
        MoveState blackHoleEdge = new MoveState("BLACK_HOLE_EDGE", BlackHoleEdgeMove,
            new SingleAttackIntent(BlackHoleEdgeDamage), new DebuffIntent());
        MoveState charge = new MoveState("MASTER_SPARK_CHARGE", MasterSparkChargeMove,
            new DefendIntent(), new BuffIntent());
        MoveState masterSpark = new MoveState("MASTER_SPARK", MasterSparkMove,
            new SingleAttackIntent(MasterSparkDamage), new BuffIntent());

        // 随机分支：星辰幻想 / 黑洞边缘（均不可连续使用）
        // RandomBranchState randomBranch = new RandomBranchState("RAND_BRANCH");
        // randomBranch.AddBranch(stellarFantasy, MoveRepeatType.CannotRepeat);
        // randomBranch.AddBranch(blackHoleEdge, MoveRepeatType.CannotRepeat);

        // 蘑菇分支：蘑菇少于上限时召唤，否则直接路由到随机分支（不空过）
        ConditionalBranchState mushroomBranch = new ConditionalBranchState("MUSHROOM_BRANCH");
        mushroomBranch.AddState(fungusExpert, () => AliveMushroomCount < MaxMushroomCount);
        mushroomBranch.AddState(stellarFantasy, () => AliveMushroomCount >= MaxMushroomCount);

        // 分支后判定：星辰幻想与黑洞边缘都用过一次后，必定进入极限火花·蓄力
        // ConditionalBranchState afterBranch = new ConditionalBranchState("AFTER_BRANCH");
        // afterBranch.AddState(charge, () => _hasUsedStellarFantasy && _hasUsedBlackHoleEdge);
        // afterBranch.AddState(randomBranch, () => !_hasUsedStellarFantasy || !_hasUsedBlackHoleEdge);

        escapeVelocity.FollowUpState = mushroomBranch;
        fungusExpert.FollowUpState = stellarFantasy;
        stellarFantasy.FollowUpState = blackHoleEdge;
        blackHoleEdge.FollowUpState = charge;
        charge.FollowUpState = masterSpark;
        masterSpark.FollowUpState = escapeVelocity;

        list.Add(escapeVelocity);
        list.Add(fungusExpert);
        list.Add(stellarFantasy);
        list.Add(blackHoleEdge);
        list.Add(charge);
        list.Add(masterSpark);
        list.Add(mushroomBranch);
        // list.Add(randomBranch);
        // list.Add(afterBranch);

        return new MonsterMoveStateMachine(list, escapeVelocity);
    }

    // --- 技能方法 ---

    /// <summary>
    /// 逃逸速度：偷走每个玩家弃牌堆顶的牌，然后造成伤害。
    /// 弃牌堆为空则不偷，技能正常执行。
    /// </summary>
    private async Task EscapeVelocityMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/thieving_hopper/thieving_hopper_steal");
        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
        List<CardModel> stolenCards = new List<CardModel>();
        foreach (Creature target in targets)
        {
            Player? player = target.Player;
            if (target.IsDead || player == null) continue;

            CardPile discardPile = PileType.Discard.GetPile(player);
            CardModel? cardToSteal = discardPile.Cards.LastOrDefault();
            if (cardToSteal == null) continue;

            await CardPileCmd.RemoveFromCombat(cardToSteal);

            // 创建"我会还给你的"能力实例暂存这张被偷的牌（每名玩家最多 1 张，每个实例 1 张）
            IWillReturnItPower returnPower = (IWillReturnItPower)ModelDb.Power<IWillReturnItPower>().ToMutable();
            returnPower.StolenCard = cardToSteal;
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), returnPower, base.Creature, 1m, base.Creature, null);

            stolenCards.Add(cardToSteal);
        }

        // 卡牌从牌堆中飞出的节奏：等待后让被偷的牌显示在魔理沙身后（仿 ThievingHopper）
        await Cmd.Wait(0.6f);
        TalkCmd.Play(_thiefLine, base.Creature, VfxColor.Gold, VfxDuration.VeryLong);
        foreach (CardModel card in stolenCards)
        {
            if (creatureNode == null || !LocalContext.IsMine(card)) continue;

            Marker2D? stolenCardPos = creatureNode.GetSpecialNode<Marker2D>("%StolenCardPos");
            if (stolenCardPos == null) continue;

            NCard? nCard = NCard.Create(card);
            if (nCard == null) continue;

            stolenCardPos.AddChildSafely(nCard);
            nCard.Position += nCard.Size * 0.5f;
            nCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
            _stolenCardNodes.Add(nCard);
        }

        await DamageCmd.Attack(EscapeVelocityDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>
    /// 菌类专家：召唤 2 只奇幻蘑菇（受场上存活蘑菇数量上限限制）。
    /// </summary>
    private async Task FungusExpertMove(IReadOnlyList<Creature> targets)
    {
        TalkCmd.Play(_mushroomLine, base.Creature, VfxColor.White, VfxDuration.Long);
        int toSummon = Math.Min(FungusExpertSummonCount, MaxMushroomCount - AliveMushroomCount);
        for (int i = 0; i < toSummon; i++)
        {
            await SummonMushroom();
        }
    }

    /// <summary>
    /// 星辰幻想：4 段多段攻击。
    /// </summary>
    private async Task StellarFantasyMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StellarFantasyDamage)
            .FromMonster(this)
            .WithHitCount(StellarFantasyHits)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>
    /// 黑洞边缘：造成伤害并给玩家施加虚弱。
    /// </summary>
    private async Task BlackHoleEdgeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BlackHoleEdgeDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, BlackHoleEdgeWeak, base.Creature, null);
    }

    /// <summary>
    /// 极限火花·蓄力：获得格挡，归还偷走的牌到手牌，每个存活蘑菇提供 10 点活力。
    /// </summary>
    private async Task MasterSparkChargeMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(base.Creature, MasterSparkChargeBlock, ValueProp.Unpowered, null);
        await ReturnStolenCards();

        TalkCmd.Play(_prepareLine, base.Creature, VfxColor.Gold, VfxDuration.VeryLong);
        int vigorAmount = base.Creature.GetPower<MagicianPower>()?.Amount ?? MasterSparkChargeVigorPerMushroom;
        vigorAmount *= AliveMushroomCount;
        if (vigorAmount > 0)
        {
            await PowerCmd.Apply<VigorPower>(new ThrowingPlayerChoiceContext(), base.Creature, vigorAmount, base.Creature, null);
        }
    }

    /// <summary>
    /// 极限火花：造成伤害并获得力量。
    /// </summary>
    private async Task MasterSparkMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(MasterSparkDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, MasterSparkStrength, base.Creature, null);
    }

    /// <summary>
    /// 在空槽位召唤一只奇幻蘑菇，并附加仆从与菌类能力。
    /// </summary>
    private async Task SummonMushroom()
    {
        string? slot = base.CombatState.Encounter?.Slots.LastOrDefault(
            s => base.CombatState.Enemies.All(c => c.SlotName != s));
        if (slot == null) return;

        Creature mushroom = await CreatureCmd.Add(ModelDb.Monster<FantasyMushroomMonster>().ToMutable(), base.CombatState, CombatSide.Enemy, slot);
    }

    /// <summary>
    /// 将魔理沙偷走的牌归还到对应玩家手牌。
    /// </summary>
    private async Task ReturnStolenCards()
    {
        // 清除魔理沙身后显示的偷取卡牌节点：归还后不再展示，直到下次偷取卡牌时重新创建
        foreach (NCard stolenCardNode in _stolenCardNodes)
        {
            stolenCardNode.QueueFreeSafely();
        }
        _stolenCardNodes.Clear();

        // 获取魔理沙身上所有"我会还给你的"能力实例（每个实例暂存一名玩家被偷的 1 张牌）
        List<IWillReturnItPower> returnPowers = base.Creature.GetPowerInstances<IWillReturnItPower>().ToList();
        if (returnPowers.Count == 0) return;

        // 归还每张被偷的牌到对应玩家手牌
        foreach (IWillReturnItPower returnPower in returnPowers)
        {
            CardModel? card = returnPower.StolenCard;
            if (card == null) continue;
            if (card.Owner == null || card.Owner.Creature == null || card.Owner.Creature.IsDead) continue;

            // 牌被移出战斗后需要先恢复其战斗状态标记，才能重新加入手牌
            card.HasBeenRemovedFromState = false;
            await CardPileCmd.Add(card, PileType.Hand);
        }

        // 归还后移除这些能力实例
        foreach (IWillReturnItPower returnPower in returnPowers)
        {
            await PowerCmd.Remove(returnPower);
        }
    }
}
