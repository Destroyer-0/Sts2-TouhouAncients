using BaseLib.Abstracts;

namespace TouhouAncients.Scripts.powers;

public abstract class TouhouAncientPowerModel:CustomPowerModel
{
    public override string? CustomPackedIconPath => TouhouAncientCmd.CheckPathExists($"res://images/icon/power/{GetType().Name}.png");
    public override string? CustomBigIconPath => TouhouAncientCmd.CheckPathExistsWithFallback2($"res://images/icon/power/BigIcon/{GetType().Name}.png",CustomPackedIconPath);
}