using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 出场动画结束后原版把名字横幅的描边与影子置为透明，浅色字压在立绘上难辨认；
/// 对本 Mod 的先古之民补回灰色描边。
/// </summary>
[HarmonyPatch(typeof(NAncientNameBanner), "_Ready")]
public static class AncientNameBannerOutlinePatch
{
    // outline_size 沿用场景的 12，只换颜色
    private static readonly Color OutlineColor = new("#5A5A5A");

    private static void Postfix(NAncientNameBanner __instance)
    {
        if (Traverse.Create(__instance).Field("_ancient").GetValue<AncientEventModel>() is not TouhouAncientBase)
            return;
        if (__instance.GetNodeOrNull<RichTextLabel>("%Title") is not { } title)
            return;

        Label? epithet = __instance.GetNodeOrNull<Label>("%Epithet");
        // 置透明发生在 await 动画结束后，没有信号可挂，用低频 Timer 等那一刻
        var timer = new Godot.Timer { WaitTime = 0.25, OneShot = false };
        timer.Timeout += () =>
        {
            if (title.GetThemeColor(ThemeConstants.RichTextLabel.FontOutlineColor).A > 0.01f)
                return;
            title.AddThemeColorOverride(ThemeConstants.RichTextLabel.FontOutlineColor, OutlineColor);
            epithet?.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, OutlineColor);
            timer.QueueFree();
        };
        __instance.AddChild(timer);
        timer.Start();
    }
}
