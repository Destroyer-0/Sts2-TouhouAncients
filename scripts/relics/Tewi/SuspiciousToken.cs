using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.Patches;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 可疑信物：在每个商店，你可以免费刷新一次商人出售的物品，刷新后打折50%！
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SuspiciousToken : TouhouAncientRelics
{
    private const string DiscountKey = "Discount";
    public const float RefreshSlotXOffset = 180f;
    public const float RefreshSlotYOffset = 40f;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar(DiscountKey, 50m) };

    /// <summary>本商店免费刷新是否已被使用。</summary>
    public bool CanRefresh => !TouhouAncients_FreeRefreshUsed;
    public bool TouhouAncients_FreeRefreshUsed { get; set; }

    /// <summary>当前商店的 MerchantRefreshEntry，供 UI 按钮查询状态。</summary>
    public MerchantRefreshEntry? RefreshEntry { get; set; }

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        if (room is MerchantRoom)
        {
            TouhouAncients_FreeRefreshUsed = false;
            RefreshEntry = null;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 刷新后所有商品价格打 (Discount)% 折。
    /// </summary>
    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        if (player != base.Owner) return originalPrice;
        if (!TouhouAncients_FreeRefreshUsed) return originalPrice;
        // 刷新入口本身价格为0，无需打折
        if (entry is MerchantRefreshEntry) return originalPrice;
        return originalPrice * (base.DynamicVars[DiscountKey].BaseValue / 100m);
    }

    /// <summary>
    /// 执行商店物品刷新（仅数据层）。由 MerchantRefreshEntry 在购买时调用。
    /// </summary>
    public async Task DoRefresh(MerchantInventory merchantInventory)
    {
        if (TouhouAncients_FreeRefreshUsed) return;
        TouhouAncients_FreeRefreshUsed = true;

        var player = base.Owner;
        if (player == null) return;

        // 1. 刷新所有卡牌（CardEntries 已包含角色+无色）
        foreach (var entry in merchantInventory.CardEntries)
            entry.Populate();

        // 2. 刷新遗物
        var relicBlacklist = merchantInventory.RelicEntries
            .Select(e => e.Model?.CanonicalInstance)
            .OfType<RelicModel>()
            .ToHashSet();

        
        var fillRelic = HarmonyLib.AccessTools.Method(
            typeof(MerchantRelicEntry),
            "FillSlot",
            new[] { typeof(RelicRarity), typeof(IEnumerable<RelicModel>) });
        if (fillRelic != null)
        {
            foreach (var entry in merchantInventory.RelicEntries)
            {
                if (entry.Model?.CanonicalInstance is RelicModel oldRelic)
                {
                    relicBlacklist.Remove(oldRelic);
                }

                var rarity = RelicFactory.RollRarity(player);
                fillRelic.Invoke(entry, new object[] { rarity, relicBlacklist });

                if (entry.Model?.CanonicalInstance is RelicModel newRelic)
                {
                    relicBlacklist.Add(newRelic);
                }
            }
        }

        // 3. 刷新药水
        var fillPotion = HarmonyLib.AccessTools.Method(
            typeof(MerchantPotionEntry),
            "FillSlot",
            new[] { typeof(IEnumerable<PotionModel>) });
        if (fillPotion != null)
        {
            var potionBlacklist = merchantInventory.PotionEntries
                .Select(e => e.Model?.CanonicalInstance)
                .OfType<PotionModel>()
                .ToHashSet();

            foreach (var entry in merchantInventory.PotionEntries)
            {
                if (entry.Model?.CanonicalInstance is PotionModel oldPotion)
                {
                    potionBlacklist.Remove(oldPotion);
                }

                fillPotion.Invoke(entry, new object[] { potionBlacklist });

                if (entry.Model?.CanonicalInstance is PotionModel newPotion)
                {
                    potionBlacklist.Add(newPotion);
                }
            }
        }

        // 4. 刷新 CardRemoval（重置已使用状态）
        if (merchantInventory.CardRemovalEntry is { } cardRemoval)
        {
            var usedField = typeof(MerchantCardRemovalEntry)
                .GetField("<Used>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (usedField != null)
            {
                usedField.SetValue(cardRemoval, false);
            }
            cardRemoval.CalcCost();
        }

        // 5. 通知所有条目更新 UI
        foreach (var entry in merchantInventory.AllEntries)
            entry.OnMerchantInventoryUpdated();

        Flash();
    }
}

/// <summary>
/// Registers SuspiciousToken's merchant refresh service as a real merchant entry
/// and renders it with the same scene tree as the vanilla card removal service.
/// </summary>
[HarmonyPatch]
public static class SuspiciousTokenPatches
{
    private static readonly ConditionalWeakTable<MerchantInventory, MerchantRefreshEntry> RefreshEntries = new();
    private static readonly ConditionalWeakTable<NMerchantInventory, NMerchantRefreshSlot> RefreshSlots = new();
    private static readonly ConditionalWeakTable<MerchantInventory, NMerchantInventory> InventoryToUI = new();

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

        // 存储 UI → Data 映射，供刷新后重置 CardRemoval UI 使用
        InventoryToUI.Remove(inventory);
        InventoryToUI.Add(inventory, __instance);

        var entry = EnsureRefreshEntry(inventory, player);
        if (entry == null) return;

        var cardRemovalNode = __instance.GetNodeOrNull<NMerchantCardRemoval>("%MerchantCardRemoval");
        var slotsContainer = __instance.GetNodeOrNull<Control>("%SlotsContainer");
        if (cardRemovalNode == null || slotsContainer == null) return;

        if (!RefreshSlots.TryGetValue(__instance, out var refreshSlot))
        {
            refreshSlot = NMerchantRefreshSlot.CreateFromCardRemovalTemplate(cardRemovalNode);
            refreshSlot.Name = "TouhouAncientsMerchantRefresh";
            refreshSlot.Position = cardRemovalNode.Position + new Vector2(SuspiciousToken.RefreshSlotXOffset, SuspiciousToken.RefreshSlotYOffset);
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

    /// <summary>
    /// 修复：NMerchantCard 购买后 Visible=false，刷新后未恢复。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantCard), "UpdateVisual")]
    [HarmonyPostfix]
    private static void AfterCardUpdateVisual(NMerchantCard __instance)
    {
        if (__instance.Entry.IsStocked)
        {
            __instance.Visible = true;
            __instance.MouseFilter = Control.MouseFilterEnum.Stop;
        }
    }

    /// <summary>
    /// 修复：NMerchantRelic 购买后 Visible=false，刷新后未恢复。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantRelic), "UpdateVisual")]
    [HarmonyPostfix]
    private static void AfterRelicUpdateVisual(NMerchantRelic __instance)
    {
        if (__instance.Entry.IsStocked)
        {
            __instance.Visible = true;
            __instance.MouseFilter = Control.MouseFilterEnum.Stop;
        }
    }

    /// <summary>
    /// 修复：NMerchantPotion 购买后 Visible=false，刷新后未恢复。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantPotion), "UpdateVisual")]
    [HarmonyPostfix]
    private static void AfterPotionUpdateVisual(NMerchantPotion __instance)
    {
        if (__instance.Entry.IsStocked)
        {
            __instance.Visible = true;
            __instance.MouseFilter = Control.MouseFilterEnum.Stop;
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

            // 刷新后重置 CardRemoval UI 状态
            if (InventoryToUI.TryGetValue(inventory, out var ui))
            {
                ResetCardRemovalUI(ui);
            }
        };
        RefreshEntries.Add(inventory, entry);
        return entry;
    }

    /// <summary>
    /// 重置 CardRemoval 的 UI 状态，使其在刷新后可再次使用。
    /// </summary>
    private static void ResetCardRemovalUI(NMerchantInventory merchantUI)
    {
        var cardRemovalNode = merchantUI.GetNodeOrNull<NMerchantCardRemoval>("%MerchantCardRemoval");
        if (cardRemovalNode == null) return;

        // 重置 _isUnavailable，使 UpdateVisual 能重新进入可用分支
        var unavailableField = typeof(NMerchantCardRemoval)
            .GetField("_isUnavailable", BindingFlags.Instance | BindingFlags.NonPublic);
        if (unavailableField != null)
        {
            unavailableField.SetValue(cardRemovalNode, false);
        }

        // 停止播放 "Used" 动画
        var animator = cardRemovalNode.GetNodeOrNull<AnimationPlayer>("%Animation");
        if (animator != null && animator.CurrentAnimation == "Used")
        {
            animator.Stop();
            animator.Seek(0.0, true);
        }

        // 通过触发 EntryUpdated 事件来间接调用 UpdateVisual（它是 protected 方法）
        merchantUI.Inventory?.CardRemovalEntry?.OnMerchantInventoryUpdated();
    }
}
