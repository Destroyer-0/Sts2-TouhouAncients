using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.badges;

/// <summary>无底之胃吞噬数 ≥10 或剜天之勺吞噬卡数 ≥16。</summary>
public class EndlessStomach : TouhouAncientBadge
{
    public EndlessStomach() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Bronze;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) =>
        GetSavedInt(player, ModelDb.Relic<BottomlessStomach>().Id, "TouhouAncients_ConsumedCount") >= 10 ||
        GetSavedInt(player, ModelDb.Relic<SkySwallowingSpoon>().Id, "TouhouAncients_SwallowedCards") >= 16;

    /// <summary>读遗物 [SavedProperty] 计数，键名即 C# 属性名。</summary>
    private static int GetSavedInt(SerializablePlayer player, ModelId relicId, string propertyName)
    {
        var ints = player.Relics.FirstOrDefault(relic => relic.Id == relicId)?.Props?.ints;
        if (ints == null) return 0;
        foreach (var saved in ints)
        {
            if (saved.name == propertyName) return saved.value;
        }
        return 0;
    }
}
