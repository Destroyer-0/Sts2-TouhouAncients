using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class Yishixingqile : TouhouAncientRelics
{
    /// <summary>被选为免费的商品列表。非保存字段，离开商店后失效。</summary>
    private readonly HashSet<MerchantEntry> _freeEntries = new();

    public override async Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        if (player != base.Owner) return;
        if (goldSpent <= 0) return; // 购买 0 价格商品不算消费

        Flash();

        // 移除已购买的商品（如果它在免费列表中）
        _freeEntries.Remove(itemPurchased);

        // 从库存中随机选一个未被标记为免费的其他商品
        if (player.RunState.CurrentRoom is MerchantRoom merchantRoom)
        {
            var candidates = merchantRoom.Inventory.AllEntries
                .Where(e => e.IsStocked
                         && e != itemPurchased
                         && !_freeEntries.Contains(e))
                .ToList();

            if (candidates.Count > 0)
            {
                _freeEntries.Add(candidates.UnstableShuffle(player.RunState.Rng.Niche).First());
            }
        }
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
    {
        if (player != base.Owner) return cost;
        if (_freeEntries.Contains(entry)) return 0m;

        return cost;
    }

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        _freeEntries.Clear();
        return Task.CompletedTask;
    }
}