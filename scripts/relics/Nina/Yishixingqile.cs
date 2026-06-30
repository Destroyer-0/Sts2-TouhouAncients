using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(EventRelicPool))]
public class Yishixingqile : TouhouAncientRelics
{
    private const int StartingGold = 112;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Gold", StartingGold)];

    /// <summary>被选为免费的商品列表。非保存字段，离开商店后失效。</summary>
    private readonly HashSet<MerchantEntry> _freeEntries = new();

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        await PlayerCmd.GainGold(StartingGold, base.Owner);
    }

    public override async Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        if (player != base.Owner) return;
        if(itemPurchased is MerchantRefreshEntry) return;
        
        // 移除已购买的商品（如果它在免费列表中）
        _freeEntries.Remove(itemPurchased);
        
        if (goldSpent <= 0) return; // 购买 0 价格商品不算消费

        Flash();


        // 从库存中随机选一个未被标记为免费的其他商品
        if (player.RunState.CurrentRoom is MerchantRoom merchantRoom)
        {
            for (int i = 0; i < player.RunState.Rng.Shuffle.NextInt(1, 3); i++)
            {
                var candidates = merchantRoom.Inventories
                    .Where(inv => inv.Player == player)
                    .SelectMany(x=>x.AllEntries)
                    .Where(e => e.IsStocked
                                && e != itemPurchased
                                && e is not MerchantRefreshEntry
                                && !_freeEntries.Contains(e))
                    .ToList();

                if (candidates.Count > 0)
                {
                    var chosen = candidates.UnstableShuffle(player.RunState.Rng.Niche).First();
                    _freeEntries.Add(chosen);
                    chosen.OnMerchantInventoryUpdated();
                    
                    var room = NMerchantRoom.Instance;
                    if (LocalContext.IsMe(base.Owner) &&  room!=null )
                    {
                        var inventory = room.Inventory;
                        NMerchantSlot slot = inventory.GetAllSlots().FirstOrDefault(s => s.Entry == chosen);
                        VfxCmd.PlayNonCombatVfx(room, slot.GlobalPosition,
                            "vfx/vfx_starry_impact");
                    }
                }
            }
        }
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
    {
        if (player != base.Owner) return cost;

        if (!LocalContext.IsMe(base.Owner))
        {
            return cost;
        }
        
        if (_freeEntries.Contains(entry)) return 0m;
        return cost;
    }

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        _freeEntries.Clear();
        return Task.CompletedTask;
    }
}