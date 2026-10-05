using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 境界之梦：拾起时，失去 17 点生命。你可以无视当前的路线选择下一层的「?」房间或商店房间
/// （其余节点仍需走正常路线）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BoundaryDream : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HpLossVar(17m)];

    public override async Task AfterObtained()
    {
        if (base.Owner?.Creature == null) return;
        if (base.DynamicVars.HpLoss.BaseValue <= 0) return;

        Flash();
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            base.Owner.Creature,
            base.DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
    }
}

/// <summary>
/// 把「无视路线」收窄成只额外解锁下一层的「?」（Unknown）与商店节点：基础可移动集合上追加这两类节点。
/// 没有逐节点的官方 Hook，只能补 <see cref="MapTravel.GetTravelablePointsFrom"/>。
/// </summary>
[HarmonyPatch]
public static class BoundaryDreamPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapTravel), nameof(MapTravel.GetTravelablePointsFrom))]
    private static void AddUnknownAndShopPoints(IRunState runState, MapPoint currentPoint, ref IEnumerable<MapPoint> __result)
    {
        if (runState.Players.All(p => p.GetRelic<BoundaryDream>() == null)) return;

        var extra = runState.Map.GetPointsInRow(currentPoint.coord.row + 1)
            .Where(p => p.PointType is MapPointType.Unknown or MapPointType.Shop);

        __result = __result.Concat(extra).Distinct();
    }
}
