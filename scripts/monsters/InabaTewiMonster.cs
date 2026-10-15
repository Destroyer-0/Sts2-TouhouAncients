using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 因幡帝：用「幸运」戏法搅乱玩家耗能的兔子诈骗师。
/// 状态机：开运大纹 → 狡兔三窟 → 请君入瓮 →（回环）。
/// 诈术师让耗能不等于目标费用的攻击牌伤害减半，狡兔三窟每轮重置目标费用并塞牌，
/// 玩家打出的戏法越多，请君入瓮给的力量越高。
/// </summary>
public sealed class InabaTewiMonster : TouhouAncientMonsterBase
{
    /// <summary>诈术师目标费用的取值个数（0~3）。</summary>
    private const int TargetCostValueCount = 4;

    private const int TricksterInitialTargetCost = 1;
    private const int LuckyTrickCardsPerMove = 2;
    private const int TrapBaseStrength = 2;

    // --- 本地化 ---
    private static readonly LocString _intoTheTrapLine =
        new LocString("monsters", "TOUHOUANCIENTS-INABA_TEWI_MONSTER.moves.INTO_THE_TRAP.banter");

    protected override bool HasAnimation => false;

    protected override int InitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 195, 185);

    private int GreatFortuneCrestDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);

    private int IntoTheTrapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    private int ThreeBurrowsBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);

    /// <summary>自身身上的诈术师，用于重置目标费用（不能走 PowerCmd.ModifyAmount，见 TricksterPower）。</summary>
    private TricksterPower? _trickster;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        _trickster = await PowerCmd.Apply<TricksterPower>(
            new ThrowingPlayerChoiceContext(), base.Creature, TricksterInitialTargetCost, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState greatFortuneCrest = new MoveState("GREAT_FORTUNE_CREST", GreatFortuneCrestMove,
            new SingleAttackIntent(GreatFortuneCrestDamage));
        MoveState threeBurrows = new MoveState("THREE_BURROWS", ThreeBurrowsMove,
            new StatusIntent(LuckyTrickCardsPerMove), new DefendIntent());
        MoveState intoTheTrap = new MoveState("INTO_THE_TRAP", IntoTheTrapMove,
            new SingleAttackIntent(IntoTheTrapDamage), new BuffIntent());

        greatFortuneCrest.FollowUpState = threeBurrows;
        threeBurrows.FollowUpState = intoTheTrap;
        intoTheTrap.FollowUpState = greatFortuneCrest;

        return new MonsterMoveStateMachine([greatFortuneCrest, threeBurrows, intoTheTrap], greatFortuneCrest);
    }

    private async Task GreatFortuneCrestMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(GreatFortuneCrestDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>狡兔三窟：获得格挡，给每位玩家塞牌并重置目标费用。</summary>
    private async Task ThreeBurrowsMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(base.Creature, ThreeBurrowsBlock, ValueProp.Move, null);

        foreach (Creature target in targets)
        {
            Player? player = target.Player;
            if (player == null) continue;

            await CardPileCmd.AddToCombatAndPreview<LuckyTrick>(
                player.Creature, PileType.Hand, LuckyTrickCardsPerMove, null, CardPilePosition.Top);
        }

        ResetTargetCost();
    }

    private async Task IntoTheTrapMove(IReadOnlyList<Creature> targets)
    {
        TalkCmd.Play(_intoTheTrapLine, base.Creature, VfxColor.White, VfxDuration.VeryLong);

        await DamageCmd.Attack(IntoTheTrapDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), base.Creature, TrapBaseStrength + AvgPlayedLuckyTrickCount,
            base.Creature, null);
    }

    /// <summary>玩家每打出一张「幸运」戏法，给该玩家叠 1 层计数（首次打出时才创建图标）。</summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card is not LuckyTrick) return;
        if (cardPlay.Card.Owner is not { } owner) return;

        await PowerCmd.Apply<LuckyTrickCountPower>(choiceContext, owner.Creature, 1m, base.Creature, null);
    }

    /// <summary>全场「幸运」戏法计数总和的平均值（向上取整），多人下不会随人数线性膨胀。</summary>
    private int AvgPlayedLuckyTrickCount
    {
        get
        {
            IReadOnlyList<Player> players = base.CombatState.Players;
            if (players.Count == 0) return 0;

            int total = players.Sum(player => player.Creature.GetPower<LuckyTrickCountPower>()?.Amount ?? 0);
            return (total + players.Count - 1) / players.Count;
        }
    }

    /// <summary>目标费用重置为 0~3 中与当前不同的值：(current + 1~3) % 4 必然不同且三个值等概率。</summary>
    private void ResetTargetCost()
    {
        if (_trickster == null) return;

        _trickster.ResetAmount((_trickster.Amount + 1 + base.Rng.NextInt(TargetCostValueCount - 1)) % TargetCostValueCount);
    }
}
