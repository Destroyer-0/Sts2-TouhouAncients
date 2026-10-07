using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.encounters;

namespace TouhouAncients.Scripts.badges;

/// <summary>使用铁甲战士在本局进入过辉夜挑战战斗并通关。</summary>
public class RevengeFireIronclad : TouhouAncientBadge
{
    public RevengeFireIronclad() : base(requiresWin: true, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Silver;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) =>
        player.CharacterId == ModelDb.Character<Ironclad>().Id && FoughtKaguya(run);

    private static bool FoughtKaguya(SerializableRun run)
    {
        ModelId kaguya = ModelDb.Encounter<HouraisanKaguyaEncounter>().Id;
        return AllRooms(run).Any(room => room.ModelId == kaguya);
    }
}
