using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.badges;

/// <summary>打出全人类的绯想天后开始计数，统计之后自动打出的牌数，战斗结束时判定。</summary>
public class AllHumansFlyToTheSky : TouhouAncientBadge
{
    internal const string FlagId = "touhouancients-allhumansflytothesky";

    public const int RequiredCards = 24;

    public AllHumansFlyToTheSky() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Gold;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) => BadgeFlags.Get(player, FlagId);

    public static void Unlock(Player player) => BadgeFlags.Set(player, FlagId);

    public static void RegisterSave() => BadgeFlags.Register(FlagId);
}
