using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Timeline;
using TouhouAncients.Scripts.Timeline;

namespace TouhouAncients.Scripts.Patches;

[HarmonyPatch(typeof(EpochModel), "get_PackedPortraitPath")]
internal static class EpochPackedPortraitPathPatch
{
    public static bool Prepare() => AccessTools.PropertyGetter(typeof(EpochModel), "PackedPortraitPath") != null;

    [HarmonyPostfix]
    private static void Postfix(EpochModel __instance, ref string __result)
    {
        if (__instance is not TouhouAncientTimeline timeline)
        {
            return;
        }

        string replacement = timeline.ResolveSlotPortraitPath();
        if (replacement.Length > 0)
        {
            __result = replacement;
        }
    }
}

[HarmonyPatch(typeof(EpochModel), "get_ResolvedPortraitPath")]
internal static class EpochResolvedPortraitPathPatch
{
    public static bool Prepare() => AccessTools.PropertyGetter(typeof(EpochModel), "ResolvedPortraitPath") != null;

    [HarmonyPostfix]
    private static void Postfix(EpochModel __instance, ref string __result)
    {
        if (__instance is not TouhouAncientTimeline timeline)
        {
            return;
        }

        string replacement = timeline.ResolveDetailPortraitPath();
        if (replacement.Length > 0)
        {
            __result = replacement;
        }
    }
}

// 自建列不在 eras 表里，Year 直接读会抛 LocException，改成复用门扉年份。
[HarmonyPatch(typeof(EpochModel), "get_Year")]
internal static class EpochYearPatch
{
    public static bool Prepare() => AccessTools.PropertyGetter(typeof(EpochModel), "Year") != null;

    [HarmonyPostfix]
    private static void Postfix(EpochModel __instance, ref string __result)
    {
        if (__instance is not TouhouAncientTimeline)
        {
            return;
        }

        if (!TouhouAncientTimelineRegistry.TryGetColumnNameKey(__instance.Era, out string? nameKey) || nameKey == null)
        {
            return;
        }

        LocString loc = new("eras", nameKey + ".year");
        if (loc.Exists())
        {
            __result = loc.GetFormattedText();
        }
    }
}

[HarmonyPatch(typeof(NTimelineScreen), nameof(NTimelineScreen.GetEraIcon))]
internal static class TimelineEraIconPatch
{
    public static bool Prepare() => AccessTools.Method(typeof(NTimelineScreen), nameof(NTimelineScreen.GetEraIcon)) != null;

    [HarmonyPrefix]
    private static bool Prefix(EpochEra era, ref (Texture2D Texture, string Name) __result)
    {
        if (!TouhouAncientTimelineRegistry.TryGetColumnNameKey(era, out string? nameKey) || nameKey == null)
        {
            return true;
        }

        // 自建列没有自己的图标资源，直接复用门扉图标与名称
        int doorEra = (int)EpochEra.Invitation0;
        string path = ImageHelper.GetImagePath($"atlases/era_atlas.sprites/era_{doorEra}.tres");
        Texture2D texture = PreloadManager.Cache.GetTexture2D(path);
        __result = (texture, nameKey);
        return false;
    }
}

[HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.LoadProgress))]
internal static class ProgressLoadEnsureSlotsPatch
{
    public static bool Prepare() => AccessTools.Method(typeof(ProgressSaveManager), nameof(ProgressSaveManager.LoadProgress)) != null;

    [HarmonyPostfix]
    private static void Postfix(ProgressSaveManager __instance)
        => TouhouAncientTimelineRegistry.EnsureSlots(__instance.Progress);
}

[HarmonyPatch(typeof(NTimelineScreen), nameof(NTimelineScreen.OnSubmenuOpened))]
internal static class TimelineScreenEnsureSlotsPatch
{
    public static bool Prepare() => AccessTools.Method(typeof(NTimelineScreen), nameof(NTimelineScreen.OnSubmenuOpened)) != null;

    [HarmonyPostfix]
    private static void Postfix() => TouhouAncientTimelineRegistry.EnsureSlots();
}

[HarmonyPatch(typeof(EventModel), nameof(EventModel.BeginEvent))]
internal static class EventBeginAncientUnlockPatch
{
    public static bool Prepare() => AccessTools.Method(typeof(EventModel), nameof(EventModel.BeginEvent)) != null;

    [HarmonyPostfix]
    private static void Postfix(EventModel __instance, Player player)
        => TouhouAncientTimelineRegistry.EvaluateAncientEncounter(__instance, player);
}
