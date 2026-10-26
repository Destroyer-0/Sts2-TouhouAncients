using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 侦探小说手稿：? 房间内阻止第一次生命值损伤（战斗与事件共用同一套扣血钩子），每个房间一次。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DetectiveManuscript : TouhouAncientRelics
{
    private int usedFloor = -1;

    /// <summary>消耗时所在楼层；同一地图点（含事件里推出的战斗子房间）只挡一次，读档也不重置。</summary>
    [SavedProperty]
    public int TouhouAncients_UsedFloor
    {
        get => usedFloor;
        set
        {
            AssertMutable();
            usedFloor = value;
            RefreshStatus();
        }
    }

    // 建筑师胜利房必须能打死玩家，否则终局卡住；BaseRoom 兜事件里再叠子房间的情况
    private static bool InUnknownRoom(IRunState runState)
        => runState.CurrentMapPoint?.PointType == MapPointType.Unknown
           && runState.BaseRoom?.IsVictoryRoom != true;

    private bool IsArmed
    {
        get
        {
            if (!IsMutable || base.Owner == null) return false;
            var runState = base.Owner.RunState;
            return InUnknownRoom(runState) && runState.TotalFloor != usedFloor;
        }
    }

    public override async Task AfterObtained()
    {
        // 可能在 ? 房间的事件里当场拿到
        RefreshStatus();
        // 小说集先到手时，此处完成合体
        await MergeDetectiveCollection();
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        // 原版没有「离开房间」钩子，只能进下一个房间时刷新（同皇家枕头）
        RefreshStatus();
        return Task.CompletedTask;
    }

    // 照缓冲：排在 Late，让其它减伤先结算，已减到 0 时不白吃一次
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner.Creature || !IsArmed) return amount;
        return 0m;
    }

    public override Task AfterModifyingHpLostAfterOsty()
    {
        // 只有真的改了数值才会被收进 modifiers，所以走到这里必定是这次挡下的
        TouhouAncients_UsedFloor = base.Owner.RunState.TotalFloor;
        Flash();
        return Task.CompletedTask;
    }

    /// <summary>Active = 本房间可用；Disabled = 本房间已用掉；Normal = 不在 ? 房间。</summary>
    private void RefreshStatus()
    {
        if (!IsMutable) return;
        Player? owner = base.Owner;
        if (owner == null) return;

        var runState = owner.RunState;
        base.Status = !InUnknownRoom(runState) ? RelicStatus.Normal
            : runState.TotalFloor == usedFloor ? RelicStatus.Disabled
            : RelicStatus.Active;
        InvokeDisplayAmountChanged();
    }
}
