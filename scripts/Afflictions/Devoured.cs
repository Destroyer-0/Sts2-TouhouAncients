using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;

namespace TouhouAncients.Scripts.Afflictions;

public sealed class Devoured : TouhouAncientAfflictionModel
{
    private bool _appliedExhaust;

    public bool AppliedExhaust
    {
        get { return _appliedExhaust; }
        set
        {
            AssertMutable();
            _appliedExhaust = value;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public override bool CanAfflictCardType(CardType cardType)
    {
        if ((uint)(cardType - 1) <= 1u)
        {
            return true;
        }

        return false;
    }
}