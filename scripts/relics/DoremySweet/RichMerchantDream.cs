using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 富贾之梦：你前 4 次遇到商人时，若金币少于 100 则获得 100 金币。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class RichMerchantDream : TouhouAncientRelics
{
    private const int MaxShops = 4;

    private int _shopsVisited;

    public override bool IsUsedUp => ShopsVisited >= MaxShops;
    public override bool ShowCounter => !IsUsedUp;

    // 金币够也照样消耗一次，所以显示的是「还剩几次检查」而非「还能领几次」
    public override int DisplayAmount => MaxShops - ShopsVisited;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Shops", MaxShops),
        new GoldVar(100),
        new DynamicVar("Threshold", 100),
    ];

    [SavedProperty]
    public int ShopsVisited
    {
        get => _shopsVisited;
        set
        {
            AssertMutable();
            _shopsVisited = value;
            InvokeDisplayAmountChanged();
            if (IsUsedUp) base.Status = RelicStatus.Disabled;
        }
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not MerchantRoom) return;
        if (IsUsedUp) return;

        var player = base.Owner;
        if (player == null) return;

        ShopsVisited++;

        if (player.Gold >= DynamicVars["Threshold"].IntValue) return;

        Flash();
        await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, player);
    }
}
