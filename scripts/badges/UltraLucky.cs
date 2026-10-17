using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.badges;

/// <summary>一次幸运白兔结算后，手牌中费用为 0 的牌达到指定张数。</summary>
public class UltraLucky : TouhouAncientBadge
{
    internal const string FlagId = "touhouancients-ultralucky";

    public const int RequiredZeroCostCards = 4;

    public UltraLucky() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Silver;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) => BadgeFlags.Get(player, FlagId);

    public static void Unlock(Player player) => BadgeFlags.Set(player, FlagId);

    public static void RegisterSave() => BadgeFlags.Register(FlagId);
}
