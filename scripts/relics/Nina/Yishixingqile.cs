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
    /// <summary>被选为免费的商品。非保存字段，离开商店后失效。</summary>
    private MerchantEntry? _freeEntry;

    public override async Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        if (player != base.Owner) return;
        if (goldSpent <= 0) return; // 购买 0 价格商品不算消费

        Flash();

        // 从当前库存中随机选一个商品设为免费（只选购买瞬间的，不含 Courier 后续补充）
        if (player.RunState.CurrentRoom is MerchantRoom merchantRoom)
        {
            var entries = merchantRoom.Inventory.AllEntries.ToList();
            if (entries.Count > 0)
            {
                _freeEntry = entries.UnstableShuffle(player.RunState.Rng.Niche).First();
            }
        }
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
    {
        if (player != base.Owner) return cost;
        if (_freeEntry == null || _freeEntry != entry) return cost;

        return 0m;
    }

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        if (_freeEntry != null)
        {
            _freeEntry = null;
        }
        return Task.CompletedTask;
    }
}