using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 山海之梦：拾起时，连通第一幕所有上下相邻的节点。
///
/// 每行的节点连向下一行的「同列 / 左侧最近的列 / 右侧最近的列」，与原有分叉叠加成整张图近乎全连通。
/// 只在拾取时算一次，之后靠 <see cref="ModifyGeneratedMapLate"/>（读档 / 重新生成地图）补上。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MountainSeaDream : TouhouAncientRelics
{
    // 极速地图是单列直线，没有跨列节点可连
    protected override bool BlockedInBlitz => true;

    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.RunState?.Map == null) return Task.CompletedTask;
        // 只改第一幕，别的幕地图不碰
        if (player.RunState.CurrentActIndex != 0) return Task.CompletedTask;

        if (Weave(player.RunState.Map))
        {
            Flash();
            RunManager.Instance?.InvokeMapChanged(clearDrawings: false);
        }

        return Task.CompletedTask;
    }

    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        if (actIndex == 0) Weave(map);
        return map;
    }

    /// <summary>返回是否新增了连线；Children 是 HashSet，重复调用幂等。</summary>
    private static bool Weave(ActMap map)
    {
        bool changed = false;
        int rows = map.GetRowCount();

        for (int row = 0; row < rows - 1; row++)
        {
            List<MapPoint> upper = [.. map.GetPointsInRow(row)];
            List<MapPoint> lower = [.. map.GetPointsInRow(row + 1)];
            if (upper.Count == 0 || lower.Count == 0) continue;

            SortedDictionary<int, List<MapPoint>> byCol = [];
            foreach (MapPoint to in lower)
            {
                if (!byCol.TryGetValue(to.coord.col, out List<MapPoint>? bucket))
                    byCol[to.coord.col] = bucket = [];
                bucket.Add(to);
            }

            int[] cols = [.. byCol.Keys];

            foreach (MapPoint from in upper)
            {
                int col = from.coord.col;
                int left = NearestBelow(cols, col - 1);
                int right = NearestAbove(cols, col + 1);

                if (left >= 0) changed |= Link(from, byCol[left]);
                if (byCol.TryGetValue(col, out List<MapPoint>? same)) changed |= Link(from, same);
                if (right >= 0) changed |= Link(from, byCol[right]);
            }
        }

        return changed;
    }

    private static bool Link(MapPoint from, List<MapPoint> targets)
    {
        bool changed = false;
        foreach (MapPoint to in targets)
        {
            if (from.Children.Contains(to)) continue;
            from.AddChildPoint(to);
            changed = true;
        }
        return changed;
    }

    /// <summary>≤ upperBound 的最大列号，没有则 -1。</summary>
    private static int NearestBelow(int[] cols, int upperBound)
    {
        int best = -1;
        foreach (int c in cols)
        {
            if (c > upperBound) break;
            best = c;
        }
        return best;
    }

    /// <summary>≥ lowerBound 的最小列号，没有则 -1。</summary>
    private static int NearestAbove(int[] cols, int lowerBound)
    {
        foreach (int c in cols)
            if (c >= lowerBound) return c;
        return -1;
    }
}
