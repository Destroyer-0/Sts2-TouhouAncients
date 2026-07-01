using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Afflictions;

public sealed class Devoured : AfflictionModel
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