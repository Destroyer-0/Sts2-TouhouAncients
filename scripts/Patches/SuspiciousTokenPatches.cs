using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// Registers SuspiciousToken's merchant refresh service as a real merchant entry
/// and renders it with the same scene tree as the vanilla card removal service.
/// </summary>
[HarmonyPatch]
public static class SuspiciousTokenPatches
{
    private const float RefreshSlotYOffset = -138f;

    private static readonly ConditionalWeakTable<MerchantInventory, MerchantRefreshEntry> RefreshEntries = new();
    private static readonly ConditionalWeakTable<NMerchantInventory, NMerchantRefreshSlot> RefreshSlots = new();

    [HarmonyPatch(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant))]
    [HarmonyPostfix]
    private static void AfterCreateForNormalMerchant(Player player, MerchantInventory __result)
    {
        EnsureRefreshEntry(__result, player);
    }

    [HarmonyPatch(typeof(MerchantInventory), nameof(MerchantInventory.AllEntries), MethodType.Getter)]
    [HarmonyPostfix]
    private static void IncludeRefreshEntry(MerchantInventory __instance, ref IEnumerable<MerchantEntry> __result)
    {
        if (RefreshEntries.TryGetValue(__instance, out var entry))
        {
            __result = __result.Append(entry);
        }
    }

    [HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
    [HarmonyPostfix]
    private static void AfterInventoryInitialize(NMerchantInventory __instance)
    {
        var inventory = __instance.Inventory;
        var player = inventory?.Player;
        if (inventory == null || player == null) return;

        var entry = EnsureRefreshEntry(inventory, player);
        if (entry == null) return;

        var cardRemovalNode = __instance.GetNodeOrNull<NMerchantCardRemoval>("%MerchantCardRemoval");
        var slotsContainer = __instance.GetNodeOrNull<Control>("%SlotsContainer");
        if (cardRemovalNode == null || slotsContainer == null) return;

        if (!RefreshSlots.TryGetValue(__instance, out var refreshSlot))
        {
            refreshSlot = NMerchantRefreshSlot.CreateFromCardRemovalTemplate(cardRemovalNode);
            refreshSlot.Name = "TouhouAncientsMerchantRefresh";
            refreshSlot.Position = cardRemovalNode.Position + new Vector2(0f, RefreshSlotYOffset);
            slotsContainer.AddChild(refreshSlot);
            RefreshSlots.Add(__instance, refreshSlot);
        }

        refreshSlot.Initialize(__instance);
        refreshSlot.FillSlot(entry);
        AccessTools.Method(typeof(NMerchantInventory), "UpdateNavigation")?.Invoke(__instance, null);
    }

    [HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.GetAllSlots))]
    [HarmonyPostfix]
    private static void IncludeRefreshSlot(NMerchantInventory __instance, ref IEnumerable<NMerchantSlot> __result)
    {
        if (RefreshSlots.TryGetValue(__instance, out var slot))
        {
            __result = __result.Append(slot);
        }
    }

    private static MerchantRefreshEntry? EnsureRefreshEntry(MerchantInventory inventory, Player player)
    {
        if (RefreshEntries.TryGetValue(inventory, out var existing))
        {
            return existing;
        }

        var relic = player.GetRelic<SuspiciousToken>();
        if (relic == null) return null;

        var entry = new MerchantRefreshEntry(player, relic);
        relic.RefreshEntry = entry;
        entry.PurchaseCompleted += (_, _) =>
        {
            foreach (var merchantEntry in inventory.AllEntries)
            {
                merchantEntry.OnMerchantInventoryUpdated();
            }
        };
        RefreshEntries.Add(inventory, entry);
        return entry;
    }
}
