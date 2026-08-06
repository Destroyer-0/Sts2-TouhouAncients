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
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 156, 150);

    // --- 伤害/数值 ---
    private int EscapeVelocityDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 15, 14);

    private int StellarFantasyDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 5, 4);

    private int StellarFantasyHits => 4;

    private int BlackHoleEdgeDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 16, 15);

    private int BlackHoleEdgeWeak => 2;

    private int MasterSparkChargeBlock => 14;

    private int MasterSparkChargeVigorPerMushroom => 10;

    private int MasterSparkDamage => 20;

    private int MasterSparkStrength => 2;

    private const int MaxMushroomCount = 5;

    private const int FungusExpertSummonCount = 2;
    
    private static readonly LocString _mushroomLine =
        new LocString("monsters", "TOUHOUANCIENTS-KIRISAME_MARISA_MONSTER.moves.FUNGUS_EXPERT.banter");
    
    private static readonly LocString _prepareLine =
        new LocString("monsters", "TOUHOUANCIENTS-KIRISAME_MARISA_MONSTER.moves.MASTER_SPARK_CHARGE.banter");

    /// <summary>
    /// 场上存活蘑菇的数量。
    /// </summary>
    private int AliveMushroomCount => base.CombatState.Enemies.Count(c => c is { Monster: FantasyMushroomMonster, IsDead: false });

    /// <summary>
    /// 当前循环内星辰幻想是否已使用。
    /// </summary>
    private bool _hasUsedStellarFantasy;

    /// <summary>
    /// 当前循环内黑洞边缘是否已使用。
    /// </summary>
    private bool _hasUsedBlackHoleEdge;

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
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
        // 标记本循环内星辰幻想已使用
        _hasUsedStellarFantasy = true;

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
        // 标记本循环内黑洞边缘已使用
        _hasUsedBlackHoleEdge = true;

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
        // 新一轮循环开始，重置两个分支招式的使用标记
        _hasUsedStellarFantasy = false;
        _hasUsedBlackHoleEdge = false;

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
        // 获取魔理沙身上所有"我会还给你的"能力实例（每个实例暂存一名玩家被偷的 1 张牌）
        List<IWillReturnItPower> returnPowers = base.Creature.GetPowerInstances<IWillReturnItPower>().ToList();
        if (returnPowers.Count == 0) return;

        // 归还每张被偷的牌到对应玩家手牌
        foreach (IWillReturnItPower returnPower in returnPowers)
        {
            CardModel? card = returnPower.StolenCard;
            if (card == null) continue;
            if (card.Owner == null || card.Owner.Creature == null || card.Owner.Creature.IsDead) continue;

            // 移除魔理沙身后显示的卡牌节点，避免归还后残留穿帮
            if (LocalContext.IsMine(card))
            {
                NCard.FindOnTable(card)?.QueueFreeSafely();
            }

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
