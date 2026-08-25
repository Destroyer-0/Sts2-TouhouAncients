using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.powers;

public class GeishehuaxiaojiejianshaoliliangPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("Applier")];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];
    
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ((StringVar)base.DynamicVars["Applier"]).StringValue = applier.Name;
       return PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, -Amount, null, null);
    }
}