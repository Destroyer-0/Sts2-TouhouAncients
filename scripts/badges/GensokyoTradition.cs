using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.badges;

/// <summary>同一局内打赢二层与三层的东方先古之民挑战战斗。</summary>
public class GensokyoTradition : TouhouAncientBadge
{
    public GensokyoTradition() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Silver;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) =>
        run.MapPointHistory.Count >= 3 &&
        run.MapPointHistory[1].Any(point => point.Rooms.Any(IsAncientChallengeRoom)) &&
        run.MapPointHistory[2].Any(point => point.Rooms.Any(IsAncientChallengeRoom));
}
