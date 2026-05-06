using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class Yishixingqile : TouhouAncientRelics
{
    private bool _hasTriggered;
    private bool _hasExhausted;

    public override bool IsUsedUp => TouhouAncients_HasTriggered;

    /// <summary>
    /// 触发折扣时当前商店中已陈列的所有商品快照。
    /// 只有这些商品享受价格归零，后续 Courier 刷新出的新商品不享受折扣。
    /// 非保存字段，离开当前商店后自动失效。
    /// </summary>
    private HashSet<MerchantEntry>? _discountedEntries;

    [SavedProperty]
    public bool TouhouAncients_HasTriggered
    {
        get => _hasTriggered;
        set
        {
            AssertMutable();
            _hasTriggered = value;
            if (IsUsedUp)
            {
                base.Status = RelicStatus.Disabled;
            }
        }
    }
    [SavedProperty]
    public bool TouhouAncients_HasExhausted
    {
        get => _hasExhausted;
        set
        {
            AssertMutable();
            _hasExhausted = value;
        }
    }

    public override Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        if (player != base.Owner) return Task.CompletedTask;
        if (TouhouAncients_HasTriggered) return Task.CompletedTask;
        if (TouhouAncients_HasExhausted) return Task.CompletedTask;
        if (goldSpent <= 0) return Task.CompletedTask;
        if (itemPurchased is not MerchantRelicEntry) return Task.CompletedTask;

        Flash();
        TouhouAncients_HasTriggered = true;

        // 记录当前商店中所有已陈列的商品，
        // 只有这些商品会享受价格归零，Courier 刷新出的新商品不在此列。
        if (player.RunState.CurrentRoom is MerchantRoom merchantRoom)
        {
            _discountedEntries = new HashSet<MerchantEntry>(merchantRoom.Inventory.AllEntries);
        }

        return Task.CompletedTask;
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
    {
        if (player != base.Owner) return cost;
        if (!TouhouAncients_HasTriggered) return cost;
        if (TouhouAncients_HasExhausted) return cost;
        // 只对触发时已存在的商品打折，Courier 刷新出的新商品不受影响
        if (_discountedEntries == null || !_discountedEntries.Contains(entry)) return cost;

        return 0m;
    }

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        if (TouhouAncients_HasTriggered && !TouhouAncients_HasExhausted)
        {
            TouhouAncients_HasExhausted = true;
        }
        return Task.CompletedTask;
    }
}