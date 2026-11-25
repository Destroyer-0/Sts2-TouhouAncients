using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 英文下 [b]/[i] 会回退到 Godot 默认字体，而 MegaLabelHelper 估算高度时一律按 normal_font 量，
/// 于是算出的高度小于实际排版、最后一行被顶出卡片（如 Mysterious Leaf）。
/// 这里在 AutoSize 之后用 GetContentHeight 取真实排版高度做二分兜底，只在溢出时收缩字号。
/// </summary>
[HarmonyPatch(typeof(MegaRichTextLabel), "AdjustFontSize")]
public static class MegaRichTextAutoFitPatch
{
    private static readonly FieldInfo? LastSetSizeField =
        typeof(MegaRichTextLabel).GetField("_lastSetSize", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo? SetFontSizeMethod =
        typeof(MegaRichTextLabel).GetMethod("SetFontSize", BindingFlags.Instance | BindingFlags.NonPublic);

    private static void Postfix(MegaRichTextLabel __instance)
    {
        if (SetFontSizeMethod == null || !__instance.AutoSizeEnabled)
        {
            return;
        }

        float height = __instance.Size.Y;
        if (height <= 0f)
        {
            return;
        }

        // GetContentHeight 会同步重排，拿到的是当前字号下的真实高度
        if (__instance.GetContentHeight() <= height)
        {
            return;
        }

        int current = LastSetSizeField?.GetValue(__instance) is int last && last > 0
            ? last
            : __instance.MaxFontSize;

        // 高度随字号单调不增，直接二分取最大可行字号
        int lo = __instance.MinFontSize;
        int hi = current - 1;
        int best = lo;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            SetFontSizeMethod.Invoke(__instance, new object[] { mid });
            if (__instance.GetContentHeight() > height)
            {
                hi = mid - 1;
            }
            else
            {
                best = mid;
                lo = mid + 1;
            }
        }

        SetFontSizeMethod.Invoke(__instance, new object[] { best });
    }
}
