using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TouhouAncients.Scripts.powers;

[RegisterPower(Inherit = true)]
public abstract class TouhouAncientPowerModel : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => new()
    {
        IconPath = TouhouAncientCmd.CheckPathExists($"res://images/icon/power/{GetType().Name}.png"),
        BigIconPath = TouhouAncientCmd.CheckPathExistsWithFallback2(
            $"res://images/icon/power/BigIcon/{GetType().Name}.png",
            TouhouAncientCmd.CheckPathExists($"res://images/icon/power/{GetType().Name}.png"))
    };
}