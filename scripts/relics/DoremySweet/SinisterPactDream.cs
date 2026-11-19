using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 邪契之梦：当你遇见第1个商人时，立刻获得他所出售的所有物品，然后将一张「贪婪」诅咒加入牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SinisterPactDream : TouhouAncientRelics
{
    // 极速商店是免费的三选项商店，没有「出售的所有物品」可拿
    protected override bool BlockedInBlitz => true;

    /// <summary>已进入过的商店数量。</summary>
    [SavedProperty]
    public int TouhouAncients_MerchantVisit
    {
        get => merchantVisit;
        set
        {
            AssertMutable();
            merchantVisit = value;

            if (merchantVisit >= DynamicVars["ShopIndex"].IntValue)
            {
                Status = RelicStatus.Disabled;
                InvokeDisplayAmountChanged();
            }
        }
    }

    private int merchantVisit;

    public Task? LastPurchaseEverythingTask { get; private set; }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Greed>();
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ShopIndex", 1)
    ];

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (!(room is MerchantRoom))
        {
            return Task.CompletedTask;
        }

        if (TouhouAncients_MerchantVisit >= DynamicVars["ShopIndex"].IntValue)
        {
            return Task.CompletedTask;
        }

        TouhouAncients_MerchantVisit++;
        MerchantInventory inventoryFor = RunManager.Instance.MerchantSynchronizer.GetInventoryFor(base.Owner);
        LastPurchaseEverythingTask = TaskHelper.RunSafely(PurchaseEverything(inventoryFor));
        return Task.CompletedTask;
    }

    private async Task PurchaseEverything(MerchantInventory inventory)
    {
        bool isLocked = inventory.TryBeginPurchase();
        try
        {
            await PurchaseEverythingLocked(inventory);
        }
        finally
        {
            if (isLocked)
            {
                inventory.EndPurchase();
            }
        }
    }

    private async Task PurchaseEverythingLocked(MerchantInventory inventory)
    {
        bool uiBlocked = false;
        bool ownerIsLocal = LocalContext.IsMe(base.Owner);
        try
        {
            if (TestMode.IsOff && ownerIsLocal)
            {
                NRun.Instance?.GlobalUi.TopBar.Map.Disable();
                NRun.Instance?.GlobalUi.TopBar.Deck.Disable();
                NMapScreen.Instance?.SetTravelEnabled(enabled: false);
                if (NRun.Instance != null)
                {
                    await NRun.Instance.AwaitProcessFrame();
                }
                uiBlocked = true;
                NMerchantRoom.Instance?.Inventory.BlockInput();
                await Cmd.Wait(0.75f);
                NMerchantRoom.Instance?.Inventory.Open();
                await Cmd.Wait(1f);
            }

            foreach (MerchantCardEntry characterCardEntry in inventory.CharacterCardEntries)
            {
                if (!characterCardEntry.IsStocked)
                {
                    string text = characterCardEntry.CreationResult?.Card.Id.Entry ?? "NULL";
                    Log.Warn("SinisterPactDream tried to buy an out-of-stock character card: " + text);
                }
                else
                {
                    await characterCardEntry.OnTryPurchaseUnsynchronized(inventory, ignoreCost: true);
                    await Cmd.Wait(0.25f);
                }
            }

            foreach (MerchantCardEntry colorlessCardEntry in inventory.ColorlessCardEntries)
            {
                if (!colorlessCardEntry.IsStocked)
                {
                    string text2 = colorlessCardEntry.CreationResult?.Card.Id.Entry ?? "NULL";
                    Log.Warn("SinisterPactDream tried to buy an out-of-stock colorless card: " + text2);
                }
                else
                {
                    await colorlessCardEntry.OnTryPurchaseUnsynchronized(inventory, ignoreCost: true);
                    await Cmd.Wait(0.25f);
                }
            }

            foreach (MerchantRelicEntry relicEntry in inventory.RelicEntries)
            {
                if (ownerIsLocal)
                {
                    NMerchantRoom.Instance?.Inventory.UnblockInput();
                    NRun.Instance?.GlobalUi.TopBar.Map.Enable();
                    NRun.Instance?.GlobalUi.TopBar.Deck.Enable();
                }
                await relicEntry.OnTryPurchaseUnsynchronized(inventory, ignoreCost: true);
                if (ownerIsLocal)
                {
                    NMerchantRoom.Instance?.Inventory.BlockInput();
                    NRun.Instance?.GlobalUi.TopBar.Deck.Disable();
                    NRun.Instance?.GlobalUi.TopBar.Map.Disable();
                }
                await Cmd.Wait(0.25f);
            }

            foreach (MerchantPotionEntry potionEntry in inventory.PotionEntries)
            {
                await potionEntry.OnTryPurchaseUnsynchronized(inventory, ignoreCost: true);
                await Cmd.Wait(0.25f);
            }
        }
        finally
        {
            if (uiBlocked)
            {
                NMerchantRoom.Instance?.Inventory.UnblockInput();
                NRun.Instance?.GlobalUi.TopBar.Map.Enable();
                NRun.Instance?.GlobalUi.TopBar.Deck.Enable();
                NMapScreen.Instance?.SetTravelEnabled(enabled: true);
            }
        }

        if (inventory.CardRemovalEntry != null && RunManager.Instance.IsInProgress)
        {
            if (ownerIsLocal)
            {
                NMapScreen.Instance?.SetTravelEnabled(enabled: false);
            }
            inventory.CardRemovalEntry.SetPurchaseUncancelable();
            await inventory.CardRemovalEntry.OnTryPurchaseUnsynchronized(inventory, ignoreCost: true);
            if (ownerIsLocal)
            {
                NMapScreen.Instance?.SetTravelEnabled(enabled: true);
            }
        }

        // 契约代价：领完全部商品后才塞进诅咒
        await CardPileCmd.AddCurseToDeck<Greed>(base.Owner);
    }
}
