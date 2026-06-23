using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 为可疑信物的免费商店刷新功能添加 UI 插槽。
/// 插槽在 NMerchantInventory.Initialize 时创建，确保库存打开前就已就位。
/// </summary>
[HarmonyPatch]
public static class SuspiciousTokenPatches
{
    private const string SlotName = "TouhouAncients_RefreshSlot";

    /// <summary>
    /// 在库存初始化时检查玩家是否持有可疑信物，若持有则创建刷新入口和 UI 插槽。
    /// 此时 AfterRoomEntered 尚未触发，所以直接在这里创建 MerchantRefreshEntry。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
    [HarmonyPostfix]
    private static void AfterInventoryInitialize(NMerchantInventory __instance)
    {
        var player = __instance.Inventory?.Player;
        if (player == null) return;

        var relic = player.GetRelic<SuspiciousToken>();
        if (relic == null) return;

        // 创建入口（若 AfterRoomEntered 尚未创建）
        if (relic.RefreshEntry == null)
        {
            relic.RefreshEntry = new MerchantRefreshEntry(player, relic);
        }

        var entry = relic.RefreshEntry!;

        // 查找或创建 UI 插槽
        var slotsContainer = __instance.GetNodeOrNull<Control>("%SlotsContainer");
        if (slotsContainer == null) return;

        var existing = slotsContainer.FindChild(SlotName, recursive: true, owned: false) as Control;
        if (existing != null)
        {
            existing.Visible = entry.IsStocked;
            return;
        }

        var slot = CreateRefreshSlot(entry);
        slotsContainer.AddChild(slot);
    }

    /// <summary>
    /// 每次打开库存时更新插槽可见性（如关闭后重新打开）。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Open))]
    [HarmonyPostfix]
    private static void AfterInventoryOpen(NMerchantInventory __instance)
    {
        var slotsContainer = __instance.GetNodeOrNull<Control>("%SlotsContainer");
        var slot = slotsContainer?.FindChild(SlotName, recursive: true, owned: false) as Control;
        if (slot == null) return;

        var relic = __instance.Inventory?.Player?.GetRelic<SuspiciousToken>();
        var entry = relic?.RefreshEntry;
        slot.Visible = entry != null && entry.IsStocked;
    }

    /// <summary>
    /// 关闭商店时隐藏插槽。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantInventory), "Close")]
    [HarmonyPostfix]
    private static void AfterInventoryClose(NMerchantInventory __instance)
    {
        var slotsContainer = __instance.GetNodeOrNull<Control>("%SlotsContainer");
        var slot = slotsContainer?.FindChild(SlotName, recursive: true, owned: false) as Control;
        if (slot != null)
            slot.Visible = false;
    }

    /// <summary>
    /// 创建一个类似 NMerchantCardRemoval 风格的刷新插槽。
    /// </summary>
    private static Control CreateRefreshSlot(MerchantRefreshEntry entry)
    {
        var slot = new Control();
        slot.Name = SlotName;
        slot.CustomMinimumSize = new Vector2(122f, 112f);
        slot.Size = new Vector2(122f, 112f);

        // 定位在删牌服务 (1370, 678) 上方
        slot.Position = new Vector2(1370f, 540f);

        // 背景面板
        var bg = new Panel();
        bg.Size = new Vector2(122f, 112f);
        bg.MouseFilter = Control.MouseFilterEnum.Ignore;
        var bgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.15f, 0.9f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderColor = StsColors.gold,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4
        };
        bg.AddThemeStyleboxOverride("panel", bgStyle);
        slot.AddChild(bg);

        // 图标区域 — 用带边框的 Panel 模拟
        var iconPanel = new Panel();
        iconPanel.Position = new Vector2(15f, 10f);
        iconPanel.Size = new Vector2(92f, 60f);
        iconPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        var iconStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.25f, 0.3f, 0.8f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderColor = new Color(0.3f, 0.35f, 0.4f),
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3
        };
        iconPanel.AddThemeStyleboxOverride("panel", iconStyle);
        slot.AddChild(iconPanel);

        // 图标文本 ↻
        var iconLabel = new Label();
        iconLabel.Position = new Vector2(10f, 8f);
        iconLabel.Size = new Vector2(72f, 44f);
        iconLabel.HorizontalAlignment = HorizontalAlignment.Center;
        iconLabel.VerticalAlignment = VerticalAlignment.Center;
        iconLabel.Text = "↻";
        iconLabel.AddThemeFontSizeOverride("font_size", 36);
        iconLabel.Modulate = StsColors.cream;
        iconPanel.AddChild(iconLabel);

        // 标题
        var titleLabel = new Label();
        titleLabel.Position = new Vector2(8f, 74f);
        titleLabel.Size = new Vector2(106f, 20f);
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.Text = "刷 新";
        titleLabel.AddThemeFontSizeOverride("font_size", 16);
        titleLabel.Modulate = StsColors.cream;
        slot.AddChild(titleLabel);

        // 价格标签（免费）
        var costLabel = new Label();
        costLabel.Name = "CostLabel";
        costLabel.Position = new Vector2(8f, 92f);
        costLabel.Size = new Vector2(106f, 18f);
        costLabel.HorizontalAlignment = HorizontalAlignment.Center;
        costLabel.Text = "免 费";
        costLabel.AddThemeFontSizeOverride("font_size", 14);
        costLabel.Modulate = StsColors.cream;
        slot.AddChild(costLabel);

        // 点击区域
        var hitbox = new ColorRect();
        hitbox.Size = new Vector2(122f, 112f);
        hitbox.Color = Colors.Transparent;
        hitbox.MouseFilter = Control.MouseFilterEnum.Stop;
        slot.AddChild(hitbox);

        // 悬停效果
        var hoverOverlay = new ColorRect();
        hoverOverlay.Size = new Vector2(122f, 112f);
        hoverOverlay.Color = new Color(0.3f, 0.3f, 0.4f, 0.0f);
        hoverOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        slot.AddChild(hoverOverlay);

        // 鼠标事件
        hitbox.MouseEntered += () =>
        {
            hoverOverlay.Color = new Color(0.3f, 0.3f, 0.4f, 0.3f);
            slot.Modulate = new Color(1.05f, 1.05f, 1.05f);
        };
        hitbox.MouseExited += () =>
        {
            hoverOverlay.Color = new Color(0.3f, 0.3f, 0.4f, 0.0f);
            slot.Modulate = Colors.White;
        };
        hitbox.GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            {
                _ = OnSlotClicked(entry, slot);
            }
        };

        return slot;
    }

    private static async Task OnSlotClicked(MerchantRefreshEntry entry, Control slot)
    {
        if (!entry.IsStocked) return;

        slot.Modulate = new Color(0.7f, 0.7f, 0.7f);

        var room = NMerchantRoom.Instance;
        var merchantInv = room?.Room?.GetLocalInventory();
        if (merchantInv == null) return;

        var success = await entry.OnTryPurchaseWrapper(merchantInv);
        if (success)
        {
            slot.Visible = false;
            SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
        }
        else
        {
            slot.Modulate = Colors.White;
        }
    }
}