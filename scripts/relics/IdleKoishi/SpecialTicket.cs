using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.relics.IdleKoishi;

/// <summary>
/// 特别门票：在嗨到爆下结束回合时多打一个额外回合；额外回合不进入嗨到爆，
/// 且之后每次额外回合都会把进入嗨到爆所需气氛推高 5（本场战斗内累积）。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class SpecialTicket : TouhouAncientRelics
{
    private const int ThresholdIncrease = 5;

    private bool _endedInHyped;
    private int _thresholdBonus;
    private int _suppressedTurnNumber = -1;

    /// <summary>本场战斗累积的进入阈值加成（<see cref="KoishiPerformanceCmd"/> 读取）。</summary>
    public int ThresholdBonus => _thresholdBonus;

    /// <summary>当前是否处于因额外回合而被禁止进入嗨到爆的回合。</summary>
    public bool IsHypedSuppressedThisTurn =>
        base.Owner?.PlayerCombatState != null && base.Owner.PlayerCombatState.TurnNumber == _suppressedTurnNumber;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("ThresholdIncrease", ThresholdIncrease)];

    public override Task BeforeCombatStart()
    {
        _endedInHyped = false;
        _thresholdBonus = 0;
        _suppressedTurnNumber = -1;
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        var player = base.Owner;
        if (player?.Creature == null) return Task.CompletedTask;
        if (side != player.Creature.Side || !participants.Contains(player.Creature)) return Task.CompletedTask;

        _endedInHyped = KoishiPerformanceCmd.IsHyped(player);
        return Task.CompletedTask;
    }

    public override bool ShouldTakeExtraTurn(Player player) => player == base.Owner && _endedInHyped;

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player != base.Owner) return Task.CompletedTask;

        Flash();
        _endedInHyped = false;
        _thresholdBonus += ThresholdIncrease;
        // 此刻已是额外回合（SwitchSides 先递增了回合数），记录回合号即可自动失效
        _suppressedTurnNumber = player.PlayerCombatState?.TurnNumber ?? -1;
        return Task.CompletedTask;
    }
}
