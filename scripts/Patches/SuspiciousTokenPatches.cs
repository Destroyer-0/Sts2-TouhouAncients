using System.Linq;
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
    /// 在 NMerchantInventory.Initialize 完成后添加刷新按钮。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
    [HarmonyPostfix]
    private static void AfterInventoryInitialize(NMerchantInventory __instance)
    {
        if (__instance.Inventory?.Player is not Player player) return;
        var suspiciousToken = player.GetRelic<SuspiciousToken>();
        if (suspiciousToken == null) return;
        if (__instance.FindChild(RefreshButtonName, recursive: false, owned: false) != null) return;

        AddRefreshButton(__instance, suspiciousToken);
    }

    /// <summary>
    /// 在 NMerchantInventory 中添加一个刷新按钮。
    /// </summary>
    private static void AddRefreshButton(NMerchantInventory inventory, SuspiciousToken relic)
    {
        var button = new Panel();
        button.Name = RefreshButtonName;
        button.Size = new Vector2(140f, 44f);

        // 定位：放在右下区域，返回按钮上方
        button.AnchorLeft = 0.5f;
        button.AnchorTop = 1f;
        button.AnchorRight = 0.5f;
        button.AnchorBottom = 1f;
        button.OffsetLeft = 200f;
        button.OffsetTop = -410f;
        button.OffsetRight = 340f;
        button.OffsetBottom = -366f;
        button.MouseFilter = Control.MouseFilterEnum.Stop;

        // 标签
        var label = new Label();
        label.Name = "RefreshLabel";
        label.Size = new Vector2(140f, 44f);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.Text = "刷 新";
        label.AddThemeFontSizeOverride("font_size", 22);
        label.Modulate = StsColors.cream;
        button.AddChild(label);

        // 背景样式
        var bg = new StyleBoxFlat();
        bg.BgColor = new Color(0.15f, 0.15f, 0.2f, 0.85f);
        bg.BorderWidthBottom = 2;
        bg.BorderWidthTop = 2;
        bg.BorderWidthLeft = 2;
        bg.BorderWidthRight = 2;
        bg.BorderColor = StsColors.gold;
        bg.CornerRadiusBottomLeft = 6;
        bg.CornerRadiusBottomRight = 6;
        bg.CornerRadiusTopLeft = 6;
        bg.CornerRadiusTopRight = 6;
        button.AddThemeStyleboxOverride("panel", bg);

        // 悬停高亮
        button.MouseEntered += () =>
        {
            var hoverBg = new StyleBoxFlat();
            hoverBg.BgColor = new Color(0.25f, 0.25f, 0.35f, 0.9f);
            hoverBg.BorderWidthBottom = 2;
            hoverBg.BorderWidthTop = 2;
            hoverBg.BorderWidthLeft = 2;
            hoverBg.BorderWidthRight = 2;
            hoverBg.BorderColor = StsColors.gold;
            hoverBg.CornerRadiusBottomLeft = 6;
            hoverBg.CornerRadiusBottomRight = 6;
            hoverBg.CornerRadiusTopLeft = 6;
            hoverBg.CornerRadiusTopRight = 6;
            button.AddThemeStyleboxOverride("panel", hoverBg);
        };
        button.MouseExited += () => button.AddThemeStyleboxOverride("panel", bg);

        // 点击事件
        button.GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            {
                _ = OnRefreshClicked(inventory, relic, button);
            }
        };

        inventory.AddChild(button);
        button.Visible = relic.CanRefresh;
    }

    private static async Task OnRefreshClicked(NMerchantInventory inventory, SuspiciousToken relic, Control button)
    {
        if (!relic.CanRefresh) return;

        if (await relic.TryRefresh(inventory))
        {
            button.Visible = false;
            button.MouseFilter = Control.MouseFilterEnum.Ignore;
            SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
        }
    }

    /// <summary>
    /// 每次打开商店时更新按钮状态。
    /// </summary>
    [HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Open))]
    [HarmonyPostfix]
    private static void AfterInventoryOpen(NMerchantInventory __instance)
    {
        var button = __instance.FindChild(RefreshButtonName, recursive: false, owned: false) as Panel;
        if (button == null) return;

        if (__instance.Inventory?.Player is not Player player)
        {
            button.QueueFreeSafely();
            return;
        }

        var suspiciousToken = player.GetRelic<SuspiciousToken>();
        if (suspiciousToken == null)
        {
            button.QueueFreeSafely();
            return;
        }

        button.Visible = suspiciousToken.CanRefresh;
        button.MouseFilter = suspiciousToken.CanRefresh
            ? Control.MouseFilterEnum.Stop
            : Control.MouseFilterEnum.Ignore;
    }
}