using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.badges;

/// <summary>带着 2 瓶养生汤通过第一幕。</summary>
public class HealthConscious : TouhouAncientBadge
{
    internal const string FlagId = "touhouancients-healthconscious";

    public const int RequiredPotions = 2;

    public HealthConscious() : base(requiresWin: true, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Silver;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) => BadgeFlags.Get(player, FlagId);

    public static void Unlock(Player player) => BadgeFlags.Set(player, FlagId);

    public static void RegisterSave() => BadgeFlags.Register(FlagId);
}
