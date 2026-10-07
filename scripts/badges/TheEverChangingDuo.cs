using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.badges;

/// <summary>使用封兽鵺，在二岩猯藏处获得幻灭三叉戟。</summary>
public class TheEverChangingDuo : TouhouAncientBadge
{
    public TheEverChangingDuo() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Gold;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) =>
        HasText(player.CharacterId?.Entry, "nue") &&
        HasRelic<DisillusionTrident>(player) &&
        MetMamizou(run, player.NetId);

    /// <summary>先古选择记录里出现猯藏（选项 LocString 的键以先古 id 为前缀）。</summary>
    private static bool MetMamizou(SerializableRun run, ulong netId) =>
        AllPoints(run).SelectMany(point => point.PlayerStats)
            .Where(stat => stat.PlayerId == netId)
            .SelectMany(stat => stat.AncientChoices)
            .Any(choice => HasText(choice.Title.LocEntryKey, "MAMIZOU"));
}
