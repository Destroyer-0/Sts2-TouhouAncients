using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 流星之梦：随机标记 4 处战斗，这些战斗额外掉落一件随机遗物奖励。
/// 标记机制照原版遗物「皮草大衣」（FurCoat）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MeteorDream : TouhouAncientRelics
{
    [SavedProperty]
    public int MeteorDream_ActIndex { get; private set; } = -1;

    [SavedProperty]
    private int[] MeteorDream_CoordCols { get; set; } = [];

    [SavedProperty]
    private int[] MeteorDream_CoordRows { get; set; } = [];

    [SavedProperty]
    private bool MeteorDream_CoordsSet { get; set; }

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Combats", 4m)];

    public override Task AfterObtained()
    {
        MeteorDream_ActIndex = base.Owner.RunState.CurrentActIndex;
        MarkCombats(base.Owner.RunState.Map);
        return Task.CompletedTask;
    }

    /// <summary>地图生成/读档时恢复被标记的战斗（参照 FurCoat / DragonVeinVessel）。</summary>
    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        if (actIndex != MeteorDream_ActIndex || !MeteorDream_CoordsSet)
        {
            return map;
        }

        foreach (var coord in GetMarkedCoords())
        {
            map.GetPoint(coord)?.AddQuest(this);
        }
        return map;
    }

    /// <summary>战斗奖励生成时：命中标记战斗则追加一件随机遗物奖励。</summary>
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != base.Owner) return false;
        if (room == null) return false;
        if (room.RoomType is not (RoomType.Monster or RoomType.Elite)) return false;
        if (!IsCurrentPointMarked()) return false;

        Flash();
        rewards.Add(new RelicReward(player));
        return true;
    }

    /// <summary>从本幕的普通/精英节点里洗牌抽 {Combats} 个打标记（参照 FurCoat 的 AddMarkedRooms）。</summary>
    private void MarkCombats(ActMap map)
    {
        if (MeteorDream_CoordsSet)
        {
            foreach (var coord in GetMarkedCoords())
            {
                map.GetPoint(coord)?.AddQuest(this);
            }
            return;
        }

        var rng = new Rng(base.Owner, base.Id);
        var candidates = map.GetAllMapPoints()
            .Where(p => p.PointType is MapPointType.Monster or MapPointType.Elite)
            .ToList();
        candidates.UnstableShuffle(rng);

        var picked = candidates.Take(base.DynamicVars["Combats"].IntValue).ToList();
        MeteorDream_CoordCols = new int[picked.Count];
        MeteorDream_CoordRows = new int[picked.Count];
        for (int i = 0; i < picked.Count; i++)
        {
            MeteorDream_CoordCols[i] = picked[i].coord.col;
            MeteorDream_CoordRows[i] = picked[i].coord.row;
        }
        MeteorDream_CoordsSet = true;

        foreach (var point in picked)
        {
            point.AddQuest(this);
        }
    }

    private List<MapCoord> GetMarkedCoords()
    {
        var list = new List<MapCoord>(MeteorDream_CoordCols.Length);
        for (int i = 0; i < MeteorDream_CoordCols.Length; i++)
        {
            list.Add(new MapCoord(MeteorDream_CoordCols[i], MeteorDream_CoordRows[i]));
        }
        return list;
    }

    /// <summary>当前战斗是否命中本次标记（且仍在拾起时所在幕）。</summary>
    private bool IsCurrentPointMarked()
    {
        if (!MeteorDream_CoordsSet) return false;
        if (base.Owner.RunState.CurrentActIndex != MeteorDream_ActIndex) return false;

        var coord = base.Owner.RunState.CurrentMapPoint?.coord;
        return coord != null && GetMarkedCoords().Contains(coord.Value);
    }
}
