using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 邪契之梦：当你遇见第1个商人时，立刻获得他所出售的所有物品。商店涨价至500%。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SinisterPactDream : TouhouAncientRelics
{
    /// <summary>已进入过的商店数量。</summary>
    [SavedProperty]
    public int TouhouAncients_MerchantVisit { get; set; } = 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ShopIndex", 1),
        new DynamicVar("PriceIncrease", 50)
    ];

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (!(room is MerchantRoom merchantRoom))
        {
            return Task.CompletedTask;
        }

        if (TouhouAncients_MerchantVisit >= DynamicVars["ShopIndex"].IntValue)
        {
            return Task.CompletedTask;
        }

        TouhouAncients_MerchantVisit++;
        TaskHelper.RunSafely(PurchaseEverything(merchantRoom.GetLocalInventory()));
        return Task.CompletedTask;
    }

    private async Task PurchaseEverything(MerchantInventory inventory)
    {
        if (inventory.Player != base.Owner)
        {
            return;
        }

        bool uiBlocked = false;
        try
        {
            if (TestMode.IsOff)
            {
                NRun.Instance.GlobalUi.TopBar.Map.Disable();
                NRun.Instance.GlobalUi.TopBar.Deck.Disable();
                NMapScreen.Instance.SetTravelEnabled(enabled: false);
                await NRun.Instance.AwaitProcessFrame();
                uiBlocked = true;
                NMerchantRoom.Instance.Inventory.BlockInput();
                await Cmd.Wait(0.75f);
                NMerchantRoom.Instance.Inventory.Open();
                await Cmd.Wait(1f);
            }

            foreach (MerchantCardEntry characterCardEntry in inventory.CharacterCardEntries)
            {
                if (!characterCardEntry.IsStocked)
                {
                }
                else
                {
                    await characterCardEntry.OnTryPurchaseWrapper(inventory, ignoreCost: true);
                    await Cmd.Wait(0.25f);
                }
            }

            foreach (MerchantCardEntry colorlessCardEntry in inventory.ColorlessCardEntries)
            {
                if (!colorlessCardEntry.IsStocked)
                {
                }
                else
                {
                    await colorlessCardEntry.OnTryPurchaseWrapper(inventory, ignoreCost: true);
                    await Cmd.Wait(0.25f);
                }
            }

            foreach (MerchantRelicEntry relicEntry in inventory.RelicEntries)
            {
                NRun.Instance.GlobalUi.TopBar.Map.Enable();
                NRun.Instance.GlobalUi.TopBar.Deck.Enable();
                await relicEntry.OnTryPurchaseWrapper(inventory, ignoreCost: true);
                NRun.Instance.GlobalUi.TopBar.Deck.Disable();
                NRun.Instance.GlobalUi.TopBar.Map.Disable();
                await Cmd.Wait(0.25f);
            }

            foreach (MerchantPotionEntry potionEntry in inventory.PotionEntries)
            {
                await potionEntry.OnTryPurchaseWrapper(inventory, ignoreCost: true);
                await Cmd.Wait(0.25f);
            }
        }
        finally
        {
            if (uiBlocked)
            {
                NMerchantRoom.Instance.Inventory.UnblockInput();
                NRun.Instance.GlobalUi.TopBar.Map.Enable();
                NRun.Instance.GlobalUi.TopBar.Deck.Enable();
                NMapScreen.Instance.SetTravelEnabled(enabled: true);
            }
        }

        if (inventory.CardRemovalEntry != null)
        {
            NMapScreen.Instance.SetTravelEnabled(enabled: false);
            await inventory.CardRemovalEntry.OnTryPurchaseWrapper(inventory, ignoreCost: true, cancelable: false);
            NMapScreen.Instance.SetTravelEnabled(enabled: true);
        }

        if (TouhouAncients_MerchantVisit >= DynamicVars["ShopIndex"].IntValue)
        {
            Status = RelicStatus.Disabled;
            InvokeDisplayAmountChanged();
        }
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        if (player != base.Owner) return originalPrice;
        if (!LocalContext.IsMe(base.Owner)) return originalPrice;
        // 「移除卡牌」服务不参与涨价
        if (entry is MerchantCardRemovalEntry) return originalPrice;

        var multiplier = base.DynamicVars["PriceIncrease"].BaseValue / 100m;
        return originalPrice * (1 + multiplier);
    }
}