using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.potions;

namespace TouhouAncients.Scripts.badges;

/// <summary>通关时药水栏里仍有养生药汤；requiresWin 已保证必须胜利。</summary>
public class HealthConscious : TouhouAncientBadge
{
    public HealthConscious() : base(requiresWin: true, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Silver;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player)
    {
        ModelId potionId = ModelDb.Potion<PortionedMedicinalCuisinePotion>().Id;
        return player.Potions.Any(potion => potion.Id == potionId);
    }
}
