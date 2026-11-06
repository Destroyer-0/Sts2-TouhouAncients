using System;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.badges;

/// <summary>牌组与遗物中的《幻想乡缘起》合计：2 本铜，3 本银。</summary>
public class MemoryPalace : TouhouAncientBadge
{
    private const int BronzeCount = 2;
    private const int SilverCount = 3;

    public MemoryPalace() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => Count(player) switch
    {
        >= SilverCount => BadgeRarity.Silver,
        >= BronzeCount => BadgeRarity.Bronze,
        _ => BadgeRarity.None,
    };

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) => Count(player) >= BronzeCount;

    private static int Count(SerializablePlayer player) =>
        player.Deck.Count(card => IsChronicle(card.Id?.Entry)) +
        player.Relics.Count(relic => IsChronicle(relic.Id?.Entry));

    private static bool IsChronicle(string? entry) =>
        entry != null && entry.Contains("GENSOKYO_CHRONICLE", StringComparison.OrdinalIgnoreCase);
}
