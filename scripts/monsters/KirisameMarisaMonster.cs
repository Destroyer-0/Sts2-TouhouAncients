using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 雾雨魔理沙：偷牌的魔法使。
/// 状态机：逃逸速度 → 蘑菇分支（蘑菇少于上限时召唤，否则进入随机分支）→ 极限火花·蓄力 → 极限火花 → 循环。
/// </summary>
public sealed class KirisameMarisaMonster : TouhouAncientMonster
{
    // --- HP ---
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 150, 140);

    // --- 伤害/数值 ---
    private int EscapeVelocityDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 15, 12);

    private int StellarFantasyDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 5, 4);

    private int StellarFantasyHits => 4;

    private int BlackHoleEdgeDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 16, 14);

    private int BlackHoleEdgeWeak => 2;

    private int MasterSparkChargeBlock => 11;

    private int MasterSparkChargeVigorPerMushroom => 10;

    private int MasterSparkDamage => 15;

    private int MasterSparkStrength => 2;

    private const int MaxMushroomCount = 5;

    private const int InitialMushroomCount = 2;

    private const int FungusExpertSummonCount = 2;

    /// <summary>
    /// 场上存活蘑菇的数量。
    /// </summary>
    private int AliveMushroomCount =>
        base.CombatState.Enemies.Count(c => c is { Monster: FantasyMushroom, IsDead: false });

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // 施加量 = 每蘑菇提供的活力值（Counter 类型，层数即活力值）
        await PowerCmd.Apply<MagicianPower>(new ThrowingPlayerChoiceContext(), base.Creature, MasterSparkChargeVigorPerMushroom, base.Creature, null);

        for (int i = 0; i < InitialMushroomCount; i++)
        {
            await SummonMushroom();
        }
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
        RandomBranchState randomBranch = new RandomBranchState("RAND_BRANCH");
        randomBranch.AddBranch(stellarFantasy, MoveRepeatType.CannotRepeat);
        randomBranch.AddBranch(blackHoleEdge, MoveRepeatType.CannotRepeat);

        // 蘑菇分支：蘑菇少于上限时召唤，否则直接路由到随机分支（不空过）
        ConditionalBranchState mushroomBranch = new ConditionalBranchState("MUSHROOM_BRANCH");
        mushroomBranch.AddState(fungusExpert, () => AliveMushroomCount < MaxMushroomCount);
        mushroomBranch.AddState(randomBranch, () => AliveMushroomCount >= MaxMushroomCount);

        escapeVelocity.FollowUpState = mushroomBranch;
        fungusExpert.FollowUpState = charge;
        stellarFantasy.FollowUpState = charge;
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
        list.Add(randomBranch);

        return new MonsterMoveStateMachine(list, escapeVelocity);
    }

    // --- 技能方法 ---

    /// <summary>
    /// 逃逸速度：偷走每个玩家弃牌堆顶的牌，然后造成伤害。
    /// 弃牌堆为空则不偷，技能正常执行。
    /// </summary>
    private async Task EscapeVelocityMove(IReadOnlyList<Creature> targets)
    {
        MagicianPower? magicianPower = base.Creature.GetPower<MagicianPower>();
        foreach (Creature target in targets)
        {
            Player? player = target.Player;
            if (target.IsDead || player == null) continue;

            CardPile discardPile = PileType.Discard.GetPile(player);
            CardModel? cardToSteal = discardPile.Cards.LastOrDefault();
            if (cardToSteal == null) continue;

            await CardPileCmd.RemoveFromCombat(cardToSteal);
            magicianPower?.StolenCards.Add(cardToSteal);
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

        Creature mushroom = await CreatureCmd.Add(
            ModelDb.Monster<FantasyMushroom>().ToMutable(), base.CombatState, CombatSide.Enemy, slot);
        await PowerCmd.Apply<MinionPower>(new ThrowingPlayerChoiceContext(), mushroom, 1m, base.Creature, null);
        await PowerCmd.Apply<FungalPower>(new ThrowingPlayerChoiceContext(), mushroom, 1m, base.Creature, null);
    }

    /// <summary>
    /// 将魔理沙偷走的牌归还到对应玩家手牌。
    /// </summary>
    private async Task ReturnStolenCards()
    {
        MagicianPower? magicianPower = base.Creature.GetPower<MagicianPower>();
        if (magicianPower == null || magicianPower.StolenCards.Count == 0) return;

        foreach (CardModel card in magicianPower.StolenCards.ToList())
        {
            if (card.Owner == null || card.Owner.Creature == null || card.Owner.Creature.IsDead) continue;

            // 牌被移出战斗后需要先恢复其战斗状态标记，才能重新加入手牌
            card.HasBeenRemovedFromState = false;
            await CardPileCmd.Add(card, PileType.Hand);
        }
        magicianPower.StolenCards.Clear();
    }
}
