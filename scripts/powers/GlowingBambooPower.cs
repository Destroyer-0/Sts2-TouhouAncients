using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.powers;

public class GlowingBambooPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("Card")];
    
    private class Data
    {
        public CardModel? selectedCard;
    }
    
    
    protected override object InitInternalData()
    {
        return new Data();
    }

    public void SetSelectedCard(CardModel card)
    {
        GetInternalData<Data>().selectedCard = card.CreateClone();
        ((StringVar)base.DynamicVars["Card"]).StringValue = card.Title;
    }
}