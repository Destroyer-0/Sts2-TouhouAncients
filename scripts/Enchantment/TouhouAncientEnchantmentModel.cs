using BaseLib.Abstracts;

namespace TouhouAncients.Scripts.Enchantment;

public abstract class TouhouAncientEnchantmentModel : CustomEnchantmentModel
{
    public virtual bool CanBeRandomSelected => true;
}