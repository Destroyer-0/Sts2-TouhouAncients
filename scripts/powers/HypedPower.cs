using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.powers;

/// <summary>嗨到爆：仅在场的标记状态，回合结束时清空气氛并离场。</summary>
public sealed class HypedPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (base.Owner.Player is not { } player) return;
        if (side != base.Owner.Side || !participants.Contains(base.Owner)) return;

        await KoishiPerformanceCmd.ExitHyped(choiceContext, player);
    }
}
