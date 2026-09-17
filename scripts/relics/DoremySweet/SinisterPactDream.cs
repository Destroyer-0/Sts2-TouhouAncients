using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 邪契之梦：当你遇见第1个商人时，立刻获得他所出售的所有物品。商店涨价至500%。
///
/// 实现说明：
///   - 「领取全部商品」复用原版购买流程 <see cref="MerchantEntry.OnTryPurchaseWrapper"/>（ignoreCost: true），
///     因此卡牌入牌组、遗物/药水入包、库存清空等既有逻辑全部沿用，无需自行分发。
///   - 跳过「移除卡牌」服务与「刷新商品」服务：它们是服务而非商品。
///   - 涨价对所有商品长期生效（自获得遗物起），倍率 = PriceIncrease / 100 = 5 倍，
///     与沿用原版「会员卡」的 Discount / 100 约定一致。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SinisterPactDream : TouhouAncientRelics
{
    /// <summary>已进入过的商店数量。</summary>
    private int _merchantsVisited;

    /// <summary>是否已经发放过"全部商品"奖励。</summary>
    private bool _itemsGranted;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ShopIndex", 1),
        new DynamicVar("PriceIncrease", 500)
    ];

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not MerchantRoom merchantRoom) return;

        _merchantsVisited++;

        if (_itemsGranted) return;
        if (_merchantsVisited < base.DynamicVars["ShopIndex"].IntValue) return;

        _itemsGranted = true;

        var player = base.Owner;
        if (player == null) return;

        var inventory = merchantRoom.Inventories.FirstOrDefault(inv => inv.Player == player);
        if (inventory == null) return;

        Flash();

        // 快照一份条目列表：购买过程会改动库存集合
        var entries = inventory.AllEntries
            .Where(e => e.IsStocked
                        && e is not MerchantCardRemovalEntry
                        && e is not MerchantRefreshEntry)
            .ToList();

        foreach (var entry in entries)
        {
            await entry.OnTryPurchaseWrapper(inventory, ignoreCost: true);
        }
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        if (player != base.Owner) return originalPrice;
        if (!LocalContext.IsMe(base.Owner)) return originalPrice;
        // 「移除卡牌」服务不参与涨价
        if (entry is MerchantCardRemovalEntry) return originalPrice;

        var multiplier = base.DynamicVars["PriceIncrease"].BaseValue / 100m;
        return originalPrice * multiplier;
    }
}
