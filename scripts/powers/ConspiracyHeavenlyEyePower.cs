using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.CmdUtils;

namespace TouhouAncients.Scripts.powers;

/// <summary>「先见之明」：玩家回合开始时预见。</summary>
public sealed class ConspiracyHeavenlyEyePower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientSatoriScry)];

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != base.Owner.Player) return;

        Flash();
        await SatoriScryCmd.SatoriScry(player, (int)base.Amount, choiceContext);
    }
}
