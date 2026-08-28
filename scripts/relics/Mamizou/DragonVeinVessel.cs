using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 龙脉之皿：标记1条路线。进入路线上的节点获得{Gold}金币，
/// 如果是战斗则战斗开始时获得{Strength}力量、{Dexterity}敏捷，且额外掉落{CardRewards}组卡牌奖励。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DragonVeinVessel : TouhouAncientRelics
{
    [SavedProperty]
    public int DragonVeinVessel_ActIndex { get; private set; } = -1;

    [SavedProperty]
    private int[] DragonVeinVessel_CoordCols { get; set; } = [];

    [SavedProperty]
    private int[] DragonVeinVessel_CoordRows { get; set; } = [];

    [SavedProperty]
    private bool DragonVeinVessel_CoordsSet { get; set; }

    public override bool HasUponPickupEffect => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Gold", 33m),
        new DynamicVar("Strength", 1m),
        new DynamicVar("Dexterity", 1m),
        new DynamicVar("CardRewards", 1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    /// <summary>
    /// 拾起时记录当前幕，并在地图上从起点随机游走到Boss，标记路径上所有中间节点（不含起点与Boss）。
    /// </summary>
    public override Task AfterObtained()
    {
        DragonVeinVessel_ActIndex = base.Owner.RunState.CurrentActIndex;
        MarkRoute(base.Owner.RunState.Map);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 地图生成/读档时，恢复被标记路线上的节点（参照 FurCoat）。
    /// </summary>
    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        if (actIndex != DragonVeinVessel_ActIndex || !DragonVeinVessel_CoordsSet)
        {
            return map;
        }

        foreach (var coord in GetMarkedCoords())
        {
            var point = map.GetPoint(coord);
            if (point != null)
            {
                point.AddQuest(this);
            }
        }
        return map;
    }

    /// <summary>
    /// 进入任何节点时：若当前节点位于任一玩家标记的路线上，为持有者发放金币。
    /// </summary>
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (!IsOwnerTriggered()) return;

        Flash(new[] { base.Owner.Creature });
        await PlayerCmd.GainGold(base.DynamicVars["Gold"].IntValue, base.Owner);
    }

    /// <summary>
    /// 战斗开始时：若当前战斗位于标记路线上，为持有者提供力量与敏捷。
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        if (!IsOwnerTriggered()) return;

        Flash(new[] { base.Owner.Creature });
        var creature = base.Owner.Creature;
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), creature,
            base.DynamicVars["Strength"].BaseValue, creature, null);
        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), creature,
            base.DynamicVars["Dexterity"].BaseValue, creature, null);
    }

    /// <summary>
    /// 战斗奖励生成时：若当前战斗位于标记路线上，追加额外卡牌奖励（参照 PrayerWheel）。
    /// 当前节点为精英时使用精英卡牌奖励（更高稀有度掉落）。
    /// </summary>
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != base.Owner) return false;
        if (room == null) return false;
        if (room.RoomType is not (RoomType.Monster or RoomType.Elite)) return false;
        if (!IsOwnerTriggered()) return false;

        var roomType = room.RoomType == RoomType.Elite ? RoomType.Elite : RoomType.Monster;
        int packs = base.DynamicVars["CardRewards"].IntValue;
        for (int i = 0; i < packs; i++)
        {
            rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, roomType), 3, player));
        }

        Flash(new[] { player.Creature });
        return true;
    }

    /// <summary>
    /// 从地图起点随机游走到Boss，标记路径上除起点与Boss外的所有中间节点。
    /// 随机源为确定性RNG（参照 FurCoat），不污染主线随机。
    /// </summary>
    private void MarkRoute(ActMap map)
    {
        var rng = new Rng(base.Owner, base.Id);
        var path = new List<MapPoint>();
        var current = map.StartingMapPoint;
        var bossCoord = map.BossMapPoint.coord;
        int guard = 0;
        while (current.coord != bossCoord && guard++ < 200)
        {
            var children = current.Children.ToList();
            if (children.Count == 0) break;
            current = children.UnstableShuffle(rng).First();
            if (current.coord != bossCoord)
            {
                path.Add(current);
            }
        }

        DragonVeinVessel_CoordCols = new int[path.Count];
        DragonVeinVessel_CoordRows = new int[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            DragonVeinVessel_CoordCols[i] = path[i].coord.col;
            DragonVeinVessel_CoordRows[i] = path[i].coord.row;
        }
        DragonVeinVessel_CoordsSet = true;

        foreach (var point in path)
        {
            point.AddQuest(this);
        }
    }

    /// <summary>获取本遗物标记路线的所有节点坐标。</summary>
    private List<MapCoord> GetMarkedCoords()
    {
        var list = new List<MapCoord>(DragonVeinVessel_CoordCols.Length);
        for (int i = 0; i < DragonVeinVessel_CoordCols.Length; i++)
        {
            list.Add(new MapCoord(DragonVeinVessel_CoordCols[i], DragonVeinVessel_CoordRows[i]));
        }
        return list;
    }

    /// <summary>当前坐标是否位于本遗物标记的路线（且处于拾起时所在的幕）。</summary>
    private bool IsCoordMarked(MapCoord coord)
    {
        if (!DragonVeinVessel_CoordsSet) return false;
        if (base.Owner.RunState.CurrentActIndex != DragonVeinVessel_ActIndex) return false;
        return GetMarkedCoords().Contains(coord);
    }

    /// <summary>
    /// 判断持有者是否触发本遗物效果：当前节点位于任一玩家持有的任一龙脉之皿所标记的路线上。
    /// 多个实例可各自独立触发、重复发放。
    /// </summary>
    private bool IsOwnerTriggered()
    {
        var player = base.Owner;
        var currentPoint = player.RunState.CurrentMapPoint;
        if (currentPoint == null) return false;

        // 当前坐标位于任一玩家持有的任一龙脉之皿的标记路线上
        foreach (var other in player.RunState.Players)
        {
            foreach (var relic in other.Relics)
            {
                if (relic is DragonVeinVessel vessel && vessel.IsCoordMarked(currentPoint.coord))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
