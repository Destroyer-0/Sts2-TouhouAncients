using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TouhouAncients.Scripts.Enchantment;

[RegisterEnchantment(Inherit = true)]
public abstract class TouhouAncientEnchantmentModel : ModEnchantmentTemplate
{
    public virtual bool CanBeRandomSelected => true;

    public override EnchantmentAssetProfile AssetProfile => new()
    {
        IconPath = TouhouAncientCmd.CheckPathExists($"res://images/icon/enchantment/{GetType().Name}.png")
    };
}