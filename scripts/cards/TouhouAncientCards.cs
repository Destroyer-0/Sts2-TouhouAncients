using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TouhouAncients.Scripts.cards;

[RegisterCard(typeof(EventCardPool), Inherit = true)]
public abstract class TouhouAncientCards : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://images/cards/{GetType().Name}.png"
    );

    public TouhouAncientCards(int energyCost, CardType type, CardRarity rarity, TargetType targetType,
        bool shouldShowInCardLibrary) : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }
}