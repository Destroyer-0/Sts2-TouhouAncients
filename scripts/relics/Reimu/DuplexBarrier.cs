using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 二重结界：你有2次机会无视路线选择房间。
/// 通过无视路线的方式进入战斗时，敌人将只有1点生命（对首领无效）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DuplexBarrier : TouhouAncientRelics
{
    private const int _maxUses = 2;
    private int _timesUsed;

    /// <summary>
    /// 标记当前房间是否通过无视路线进入（用于战斗开始时的HP降低）。
    /// </summary>
    private bool _wasSkippedThisRoom;

    public override bool IsUsedUp => TimesUsed >= _maxUses;
    public override bool ShowCounter => !IsUsedUp;
    public override int DisplayAmount => _maxUses - TimesUsed;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Rooms", _maxUses)
    ];

    [SavedProperty]
    public int TimesUsed
    {
        get => _timesUsed;
        set
        {
            AssertMutable();
            _timesUsed = value;
            base.DynamicVars["Rooms"].BaseValue = _maxUses - _timesUsed;
            InvokeDisplayAmountChanged();
            //CheckIfUsedUp();
        }
    }

    /// <summary>
    /// 还有剩余次数时允许无视路线。
    /// </summary>
    public override bool ShouldAllowFreeTravel() => !IsUsedUp;

    /// <summary>
    /// 进入新房间时检测是否通过无视路线进入。
    /// </summary>
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        // 重置标记（每间房间独立检测）
        _wasSkippedThisRoom = false;

        // 仅在进入新分支的第一个房间时检测（参照 WingedBoots）
        if (base.Owner.RunState.CurrentRoomCount == 1
            && base.Owner.RunState is RunState runState
            && runState.VisitedMapCoords.Count > 1)
        {
            // 获取上一个地图点的坐标
            var prevCoord = runState.VisitedMapCoords[runState.VisitedMapCoords.Count - 2];
            var prevPoint = runState.Map.GetPoint(prevCoord);
            var currentPoint = base.Owner.RunState.CurrentMapPoint;

            // 如果当前点不是上一个点的子节点 → 无视路线
            if (prevPoint != null && currentPoint != null && !prevPoint.Children.Contains(currentPoint))
            {
                _wasSkippedThisRoom = true;

                // 如果本遗物还有剩余次数，计入消耗
                if (!IsUsedUp)
                    TimesUsed++;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 战斗开始时，如果当前房间是通过无视路线进入的且非首领房，将所有敌人HP设为1。
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        if (!_wasSkippedThisRoom) return;

        // 对首领无效
        if (base.Owner.RunState.CurrentMapPoint?.PointType == MapPointType.Boss)
            return;

        Flash();
        var enemies = base.Owner.Creature.CombatState.HittableEnemies;
        foreach (var enemy in enemies)
        {
            await CreatureCmd.SetCurrentHp(enemy, 1m);
        }
    }

    /// <summary>
    /// 战斗中后续加入的敌人同样处理（如事件召唤的敌人）。
    /// </summary>
    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (creature.Side != CombatSide.Enemy) return;
        if (!_wasSkippedThisRoom) return;

        if (base.Owner.RunState.CurrentMapPoint?.PointType == MapPointType.Boss)
            return;

        Flash();
        await CreatureCmd.SetCurrentHp(creature, 1m);
    }

    // private void CheckIfUsedUp()
    // {
    //     if (IsUsedUp)
    //         base.Status = RelicStatus.Disabled;
    // }
}
