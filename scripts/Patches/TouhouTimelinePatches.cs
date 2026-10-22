using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
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

        if (TouhouAncientTimelineRegistry.TryGetEra(__instance.Era, out TouhouAncientTimelineEra? era) && era != null)
        {
            LocString loc = new("eras", era.Slug + ".year");
            if (loc.Exists())
            {
                __result = loc.GetFormattedText();
            }
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
        if (!TouhouAncientTimelineRegistry.TryGetEra(era, out TouhouAncientTimelineEra? descriptor) ||
            descriptor == null ||
            !descriptor.IsCustom)
        {
            return true;
        }

        Texture2D? texture = null;
        string iconPath = TouhouAncientTimeline.NormalizePath(descriptor.IconPath);
        if (iconPath.Length > 0 && ResourceLoader.Exists(iconPath))
        {
            texture = PreloadManager.Cache.GetTexture2D(iconPath);
        }

        texture ??= GetFallbackTexture(era);
        __result = (texture!, descriptor.Slug);
        return false;
    }

    private static Texture2D? GetFallbackTexture(EpochEra era)
    {
        EpochEra? fallback = null;
        foreach (EpochEra candidate in Enum.GetValues<EpochEra>())
        {
            if ((int)candidate <= (int)era && (fallback == null || (int)candidate > (int)fallback.Value))
            {
                fallback = candidate;
            }
        }

        if (!fallback.HasValue)
        {
            return null;
        }

        int value = (int)fallback.Value;
        string path = value >= 0
            ? ImageHelper.GetImagePath($"atlases/era_atlas.sprites/era_{value}.tres")
            : ImageHelper.GetImagePath($"atlases/era_atlas.sprites/era_minus_{Math.Abs(value)}.tres");

        return ResourceLoader.Exists(path) ? PreloadManager.Cache.GetTexture2D(path) : null;
    }
}

[HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.LoadProgress))]
internal static class ProgressLoadEnsureSlotsPatch
{
    public static bool Prepare() => AccessTools.Method(typeof(ProgressSaveManager), nameof(ProgressSaveManager.LoadProgress)) != null;

    [HarmonyPostfix]
    private static void Postfix(ProgressSaveManager __instance)
        => TouhouAncientTimelineRegistry.EnsureRootSlots(__instance.Progress);
}

[HarmonyPatch(typeof(NTimelineScreen), nameof(NTimelineScreen.OnSubmenuOpened))]
internal static class TimelineScreenEnsureSlotsPatch
{
    public static bool Prepare() => AccessTools.Method(typeof(NTimelineScreen), nameof(NTimelineScreen.OnSubmenuOpened)) != null;

    [HarmonyPostfix]
    private static void Postfix() => TouhouAncientTimelineRegistry.EnsureRootSlots();
}

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.UpdateProgressWithRunData))]
internal static class SaveManagerRunEndUnlockPatch
{
    public static bool Prepare() => AccessTools.Method(typeof(SaveManager), nameof(SaveManager.UpdateProgressWithRunData)) != null;

    [HarmonyPostfix]
    private static void Postfix(SerializableRun serializableRun, bool victory)
        => TouhouAncientTimelineRegistry.EvaluateRunEnd(serializableRun, victory);
}

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.UpdateProgressAfterCombatWon))]
internal static class SaveManagerCombatEndUnlockPatch
{
    public static bool Prepare() => AccessTools.Method(typeof(SaveManager), nameof(SaveManager.UpdateProgressAfterCombatWon)) != null;

    [HarmonyPostfix]
    private static void Postfix(Player localPlayer, CombatRoom combatRoom)
        => TouhouAncientTimelineRegistry.EvaluateCombatEnd(localPlayer, combatRoom);
}

[HarmonyPatch(typeof(EventModel), nameof(EventModel.BeginEvent))]
internal static class EventBeginAncientUnlockPatch
{
    public static bool Prepare() => AccessTools.Method(typeof(EventModel), nameof(EventModel.BeginEvent)) != null;

    [HarmonyPostfix]
    private static void Postfix(EventModel __instance, Player player)
        => TouhouAncientTimelineRegistry.EvaluateAncientEncounter(__instance, player);
}
