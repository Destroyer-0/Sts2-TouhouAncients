using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.badges;

/// <summary>击败纯狐时，绀珠悯悼一次复活都没有触发。</summary>
public class Flawless : TouhouAncientBadge
{
    internal const string FlagId = "touhouancients-flawless";

    public Flawless() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Gold;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) => BadgeFlags.Get(player, FlagId);

    public static void Unlock(Player player) => BadgeFlags.Set(player, FlagId);

    public static void RegisterSave() => BadgeFlags.Register(FlagId);
}
