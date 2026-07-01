using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts;

//namespace TouhouAncients.Scripts.relics;

public abstract class TouhouAncientRelics : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new RelicAssetProfile
    {
        IconPath = TouhouAncientCmd.CheckPathExistsWithFallback(
            $"res://images/icon/relics/{GetType().Name.ToLowerInvariant()}.png",
            $"res://images/icon/relics/default.png"),
        IconOutlinePath = TouhouAncientCmd.CheckPathExistsWithFallback(
            $"res://images/icon/relics/{GetType().Name.ToLowerInvariant()}.png",
            $"res://images/icon/relics/default.png"),
        BigIconPath = TouhouAncientCmd.CheckPathExistsWithFallback(
            $"res://images/icon/relics/IconLarge/{GetType().Name.ToLowerInvariant()}.png",
            TouhouAncientCmd.CheckPathExistsWithFallback(
                $"res://images/icon/relics/{GetType().Name.ToLowerInvariant()}.png",
                $"res://images/icon/relics/IconLarge/default.png"))
    };
}