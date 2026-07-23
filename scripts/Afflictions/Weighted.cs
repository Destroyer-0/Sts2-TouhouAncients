using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace TouhouAncients.Scripts.Afflictions;


public sealed class Weighted : TouhouAncientAfflictionModel
{
    public override bool HasExtraCardText => true;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HasCard ? [HoverTipFactory.ForEnergy(base.Card)] : [];

    public override async Task OnPlay(PlayerChoiceContext choiceContext, Creature? target)
    {
        await PlayerCmd.LoseEnergy(base.Amount, base.Card.Owner);
    }
}