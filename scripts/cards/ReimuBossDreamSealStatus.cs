using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

[Pool(typeof(StatusCardPool))]
public abstract class ReimuBossDreamSealStatus : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Status;
    private const TargetType targetType = TargetType.None;
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false; 

    protected ReimuBossDreamSealStatus(bool shouldShowInCardLibrary) : base(energyCost, type, rarity, targetType,
        shouldShowInCardLibrary)
    {
    }

    private int _fakeUpgradeLevel;

    private int FakeUpgradeLevel
    {
        get { return _fakeUpgradeLevel; }
        set
        {
            AssertMutable();
            _fakeUpgradeLevel = value;
        }
    }

    public override string Title
    {
        get
        {
            string title = base.Title;
            if (FakeUpgradeLevel > 0)
            {
                return $"{title}+{FakeUpgradeLevel}";
            }

            return title;
        }
    }

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Amount", 1m)];
    
    
    public void FakeUpgrade()
    {
        FakeUpgradeLevel++;
        base.DynamicVars["Amount"].UpgradeValueBy(1m);
    }
}