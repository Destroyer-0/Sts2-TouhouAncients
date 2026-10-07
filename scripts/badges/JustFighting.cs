using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.badges;

/// <summary>打赢先古之民挑战战斗，但把该次挑战给出的遗物奖励全部跳过。</summary>
public class JustFighting : TouhouAncientBadge
{
    public JustFighting() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Silver;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player)
    {
        // 打赢挑战必进下一节点，故挑战所在地图点不是最后一个；打输则停在挑战房间（即最后一个）。
        var lastPoint = run.MapPointHistory.LastOrDefault()?.LastOrDefault();
        return AllPoints(run).Any(point =>
            point != lastPoint &&
            point.Rooms.Any(IsAncientChallengeRoom) &&
            !TookAnyRelic(point, player.NetId));
    }

    /// <summary>该地图点里本玩家是否取走过遗物（挑战奖励页取走任意一个即算）。</summary>
    private static bool TookAnyRelic(MapPointHistoryEntry point, ulong netId)
    {
        var stats = point.PlayerStats.FirstOrDefault(stat => stat.PlayerId == netId);
        return stats != null && stats.RelicChoices.Any(choice => choice.wasPicked);
    }
}
