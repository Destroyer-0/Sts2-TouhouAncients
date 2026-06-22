using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 为可疑信物添加商店刷新按钮。
/// </summary>
[HarmonyPatch]
public static class SuspiciousTokenPatches
{
    private const string RefreshButtonName = "TouhouAncients_RefreshButton";

    /// <summary>
    /// 查找当前玩家的可疑信物遗物。
    /// </summary>
    private static SuspiciousToken? GetSuspiciousToken(NMerchantInventory inventory)
    {
        if (inventory.Inventory?.Player is not Player player) return null;
        return player.GetRelic<SuspiciousToken>();
    }

    /// <summary>
    /// 在 NMerchantRoom._Ready 之后添加刷新按钮到房间层级。
    /// NMerchantRoom 是一个全屏 Control，作为按钮的父级更可靠。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantRoom), nameof(NMerchantRoom._Ready))]
    [HarmonyPostfix]
    private static void AfterRoomReady(NMerchantRoom __instance)
    {
        var inventory = __instance.Inventory;
        if (inventory == null) return;
        var relic = GetSuspiciousToken(inventory);
        if (relic == null) return;
        if (__instance.FindChild(RefreshButtonName, recursive: true, owned: false) != null) return;

        var button = CreateRefreshButton(inventory, relic);
        __instance.AddChild(button);
        // 初始隐藏，等打开商店时再显示
        button.Visible = false;
    }

    /// <summary>
    /// 创建刷新按钮。
    /// </summary>
    private static Button CreateRefreshButton(NMerchantInventory inventory, SuspiciousToken relic)
    {
        var button = new Button();
        button.Name = RefreshButtonName;
        button.Text = "刷 新";

        // 定位：右下区域，位于返回按钮上方
        button.AnchorLeft = 1f;
        button.AnchorTop = 1f;
        button.AnchorRight = 1f;
        button.AnchorBottom = 1f;
        button.OffsetLeft = -340f;
        button.OffsetTop = -410f;
        button.OffsetRight = -200f;
        button.OffsetBottom = -366f;

        // 按钮主题样式
        var normalStyle = new StyleBoxFlat();
        normalStyle.BgColor = new Color(0.15f, 0.15f, 0.2f, 0.85f);
        normalStyle.BorderWidthBottom = 2;
        normalStyle.BorderWidthTop = 2;
        normalStyle.BorderWidthLeft = 2;
        normalStyle.BorderWidthRight = 2;
        normalStyle.BorderColor = StsColors.gold;
        normalStyle.CornerRadiusBottomLeft = 6;
        normalStyle.CornerRadiusBottomRight = 6;
        normalStyle.CornerRadiusTopLeft = 6;
        normalStyle.CornerRadiusTopRight = 6;

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor = new Color(0.25f, 0.25f, 0.35f, 0.9f);
        hoverStyle.BorderWidthBottom = 2;
        hoverStyle.BorderWidthTop = 2;
        hoverStyle.BorderWidthLeft = 2;
        hoverStyle.BorderWidthRight = 2;
        hoverStyle.BorderColor = StsColors.gold;
        hoverStyle.CornerRadiusBottomLeft = 6;
        hoverStyle.CornerRadiusBottomRight = 6;
        hoverStyle.CornerRadiusTopLeft = 6;
        hoverStyle.CornerRadiusTopRight = 6;

        var disabledStyle = new StyleBoxFlat();
        disabledStyle.BgColor = new Color(0.1f, 0.1f, 0.12f, 0.5f);
        disabledStyle.BorderWidthBottom = 2;
        disabledStyle.BorderWidthTop = 2;
        disabledStyle.BorderWidthLeft = 2;
        disabledStyle.BorderWidthRight = 2;
        disabledStyle.BorderColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        disabledStyle.CornerRadiusBottomLeft = 6;
        disabledStyle.CornerRadiusBottomRight = 6;
        disabledStyle.CornerRadiusTopLeft = 6;
        disabledStyle.CornerRadiusTopRight = 6;

        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        button.AddThemeStyleboxOverride("pressed", hoverStyle);
        button.AddThemeStyleboxOverride("disabled", disabledStyle);

        // 字体颜色
        button.AddThemeColorOverride("font_color", StsColors.cream);
        button.AddThemeColorOverride("font_hover_color", StsColors.gold);
        button.AddThemeColorOverride("font_pressed_color", StsColors.gold);
        button.AddThemeColorOverride("font_disabled_color", new Color(0.4f, 0.4f, 0.4f));

        // 字体大小
        button.AddThemeFontSizeOverride("font_size", 22);

        // 点击事件
        button.Pressed += () => _ = OnRefreshClicked(inventory, relic, button);

        return button;
    }

    private static async Task OnRefreshClicked(NMerchantInventory inventory, SuspiciousToken relic, Button button)
    {
        if (!relic.CanRefresh) return;

        button.Disabled = true;

        if (await relic.TryRefresh(inventory))
        {
            button.Visible = false;
            SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
        }
        else
        {
            button.Disabled = false;
        }
    }

    /// <summary>
    /// 每次打开商店库存面板时更新刷新按钮状态。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Open))]
    [HarmonyPostfix]
    private static void AfterInventoryOpen(NMerchantInventory __instance)
    {
        var room = NMerchantRoom.Instance;
        if (room == null) return;

        var button = room.FindChild(RefreshButtonName, recursive: true, owned: false) as Button;
        if (button == null) return;

        var relic = GetSuspiciousToken(__instance);
        if (relic == null)
        {
            button.QueueFreeSafely();
            return;
        }

        button.Visible = relic.CanRefresh;
        button.Disabled = !relic.CanRefresh;
    }

    /// <summary>
    /// 关闭商店时隐藏按钮。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Close))]
    [HarmonyPostfix]
    private static void AfterInventoryClose(NMerchantInventory __instance)
    {
        var room = NMerchantRoom.Instance;
        if (room == null) return;

        var button = room.FindChild(RefreshButtonName, recursive: true, owned: false) as Button;
        if (button == null) return;

        button.Visible = false;
    }
}