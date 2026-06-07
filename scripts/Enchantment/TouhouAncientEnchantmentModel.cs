using BaseLib.Abstracts;

namespace TouhouAncients.Scripts.Enchantment;

public abstract class TouhouAncientEnchantmentModel : CustomEnchantmentModel
{
    public virtual bool CanBeRandomSelected => true;

    protected override string? CustomIconPath => TouhouAncientCmd.CheckPathExists($"res://images/icon/enchantment/{GetType().Name}.png");
}