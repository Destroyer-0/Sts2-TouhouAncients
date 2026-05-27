using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 星尘扫帚：初始充能2，最大充能3。你可以消耗一层充能无视当前的路线选择下一层的房间，
/// 每经过3个房间获得1充能。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class StardustBroom : TouhouAncientRelics
{
    private const int MaxCharges = 3;
    private const int InitialCharges = 2;
    private const int RoomsPerCharge = 3;

    [SavedProperty]
    public int TouhouAncients_Charges { get; set; } = InitialCharges;

    [SavedProperty]
    public int TouhouAncients_RoomsSinceLastCharge { get; set; }

    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_Charges;
    public override bool IsUsedUp => TouhouAncients_Charges <= 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Charges", InitialCharges),
        new DynamicVar("MaxCharges", MaxCharges)
    ];

    public override bool HasUponPickupEffect => false;

    public override bool ShouldAllowFreeTravel()
    {
        return TouhouAncients_Charges > 0;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (base.Owner?.RunState == null) return;

        // 房间计数：每经过3个房间获得1充能
        TouhouAncients_RoomsSinceLastCharge++;

        if (TouhouAncients_RoomsSinceLastCharge >= RoomsPerCharge && TouhouAncients_Charges < MaxCharges)
        {
            TouhouAncients_Charges++;
            TouhouAncients_RoomsSinceLastCharge = 0;
            Flash();
            InvokeDisplayAmountChanged();
            return;
        }

        // 消耗充能逻辑（参照 WingedBoots）：进入新楼层的第一个房间时消耗1充能
        if (TouhouAncients_Charges <= 0) return;
        if (!(base.Owner.RunState is RunState runState)) return;
        if (runState.CurrentRoomCount > 1) return;
        if (runState.VisitedMapCoords.Count <= 1) return;

        // 检查是否使用了自由移动（进入非默认路径的房间）
        var lastCoord = runState.VisitedMapCoords[^2];
        var lastPoint = runState.Map.GetPoint(lastCoord);
        if (lastPoint == null) return;
        if (lastPoint.Children.Contains(runState.CurrentMapPoint)) return;

        // 使用了自由移动，消耗1充能
        TouhouAncients_Charges--;
        InvokeDisplayAmountChanged();
        Flash();
    }
}
