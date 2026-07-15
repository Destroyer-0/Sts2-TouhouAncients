using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// Supports switching Ancient dialogue portraits and bubble colors based on .speaker loc keys,
/// and switching option button colors based on option index.
///
/// Each dialogue line uses the standard .ancient suffix, with an additional .speaker key
/// to indicate which variant (jyoon/shion/etc.) is speaking. This avoids patching
/// DefineDialogues/HasRepeatingSuffix/PopulateLines for key scanning — only the speaker
/// registration and portrait switching need patches.
/// </summary>
public static class AncientDialoguePortraitPatch
{
    // --- PopulateLines Patch: read .speaker key ---

    /// <summary>
    /// Postfix: after the original PopulateLines sets LineText and Speaker from the standard
    /// .ancient key, check if a .speaker key exists for this line and register the variant.
    /// </summary>
    [HarmonyPatch(typeof(AncientDialogue), nameof(AncientDialogue.PopulateLines))]
    public static class PopulateLines_Patch
    {
        static void Postfix(AncientDialogue __instance, string ancientEntry, string charEntry, int dialogueIndex)
        {
            if (!AncientSpeakerRegistry.HasProfiles(ancientEntry))
                return;

            for (int i = 0; i < __instance.Lines.Count; i++)
            {
                // Only check Ancient-speaker lines
                if (__instance.Lines[i].Speaker != AncientDialogueSpeaker.Ancient)
                    continue;

                string suffix = __instance.IsRepeating ? "r" : "";
                string speakerKey = $"{ancientEntry}.talk.{charEntry}.{dialogueIndex}-{i}{suffix}.speaker";

                if (LocString.Exists("ancients", speakerKey))
                {
                    string variantId = new LocString("ancients", speakerKey).GetRawText();
                    AncientSpeakerRegistry.SetLineVariant(__instance.Lines[i], variantId);
                }
            }
        }
    }

    // --- NEventOptionButton.Create Patch: button colors ---

    [HarmonyPatch(typeof(NEventOptionButton), nameof(NEventOptionButton.Create))]
    public static class NEventOptionButton_Create_Patch
    {
        static void Postfix(NEventOptionButton __result, EventModel eventModel, int index)
        {
            if (eventModel is TouhouAncientBase touhouAncient)
            {
                var color = touhouAncient.GetOptionButtonColor(index);
                Traverse.Create(__result).Field("_buttonColor").SetValue(color);
            }
        }
    }

    // --- SetAncientAsSpeaker Patch: switch portrait by variant ---

    [HarmonyPatch(typeof(NAncientDialogueLine), "SetAncientAsSpeaker")]
    public static class SetAncientAsSpeaker_Patch
    {
        static void Postfix(NAncientDialogueLine __instance)
        {
            var line = Traverse.Create(__instance).Field("_line").GetValue<AncientDialogueLine>();
            if (line == null)
                return;

            if (!AncientSpeakerRegistry.TryGetLineVariant(line, out var variantId))
                return;

            var ancient = Traverse.Create(__instance).Field("_ancient").GetValue<AncientEventModel>();
            if (ancient == null)
                return;

            if (!AncientSpeakerRegistry.TryGetProfile(ancient.Id.Entry, variantId, out var profile))
                return;

            var iconNode = __instance.GetNode<Control>("%AncientIcon");
            iconNode.GetNode<TextureRect>("Icon").Texture = PreloadManager.Cache.GetCompressedTexture2D(profile.IconPath);
            iconNode.GetNode<TextureRect>("Icon/Outline").Texture = PreloadManager.Cache.GetCompressedTexture2D(profile.OutlinePath);

            var dialogueColor = profile.DialogueColor;
            __instance.GetNode<Control>("%Bubble").SelfModulate = dialogueColor;
            __instance.GetNode<Control>("%DialogueTailLeft").SelfModulate = dialogueColor;
        }
    }

    // --- RunHistoryIcon Merge Patch: combine portraits for multi-speaker Ancients ---

    [HarmonyPatch(typeof(AncientEventModel), "get_RunHistoryIcon")]
    public static class RunHistoryIcon_Patch
    {
        static void Postfix(AncientEventModel __instance, ref Texture2D __result)
        {
            string ancientEntry = __instance.Id.Entry;
            if (!AncientSpeakerRegistry.HasProfiles(ancientEntry))
                return;

            __result = MergeAllVariants(ancientEntry, __result, p => PreloadManager.Cache.GetCompressedTexture2D(p.IconPath));
        }
    }

    // --- RunHistoryIconOutline Merge Patch ---

    [HarmonyPatch(typeof(AncientEventModel), "get_RunHistoryIconOutline")]
    public static class RunHistoryIconOutline_Patch
    {
        static void Postfix(AncientEventModel __instance, ref Texture2D __result)
        {
            string ancientEntry = __instance.Id.Entry;
            if (!AncientSpeakerRegistry.HasProfiles(ancientEntry))
                return;

            __result = MergeAllVariants(ancientEntry, __result, p => PreloadManager.Cache.GetCompressedTexture2D(p.OutlinePath));
        }
    }

    /// <summary>
    /// Merge all variant icons horizontally at full size (no scaling).
    /// First variant uses <paramref name="firstTex"/> (already loaded by caller),
    /// remaining variants are loaded via <paramref name="getTex"/> from SpeakerProfiles.
    /// </summary>
    private static Texture2D MergeAllVariants(string ancientEntry, Texture2D firstTex, System.Func<AncientSpeakerProfile, Texture2D> getTex)
    {
        if (firstTex == null)
            return null!;

        var variantIds = AncientSpeakerRegistry.GetVariantIds(ancientEntry).ToList();
        if (variantIds.Count < 2)
            return firstTex;

        var firstImg = firstTex.GetImage();
        int w = firstImg.GetWidth();
        int h = firstImg.GetHeight();

        var merged = Image.CreateEmpty(w * variantIds.Count, h, false, firstImg.GetFormat());
        merged.BlitRect(firstImg, new Rect2I(0, 0, w, h), new Vector2I(0, 0));

        for (int i = 1; i < variantIds.Count; i++)
        {
            if (!AncientSpeakerRegistry.TryGetProfile(ancientEntry, variantIds[i], out var profile))
                continue;
            var tex = getTex(profile);
            if (tex == null)
                continue;
            var img = tex.GetImage();
            merged.BlitRect(img, new Rect2I(0, 0, w, h), new Vector2I(w * i, 0));
        }

        return ImageTexture.CreateFromImage(merged);
    }

    private static void ResetIconSize(TextureRect textureRect, Texture2D tex)
    {
        var h = textureRect.CustomMinimumSize.Y;
        var w = tex.GetWidth() * h / tex.GetHeight();
        textureRect.CustomMinimumSize = new Vector2(w, h);
    }
    
    // --- NRelicCollectionCategory.LoadIcon Patch: expand width for merged icons ---

    [HarmonyPatch(typeof(NRelicCollectionCategory), "LoadIcon")]
    public static class LoadIcon_Patch
    {
        static void Postfix(NRelicCollectionCategory __instance, Texture2D tex)
        {
            if (tex == null)
                return;

            var icon = Traverse.Create(__instance).Field("_icon").GetValue<TextureRect>();
            ResetIconSize(icon, tex);
        }
    }
    //
    // [HarmonyPatch(typeof(NTopBarRoomIcon), "UpdateIcon")]
    // public static class UpdateIcon_Patch
    // {
    //     static void Postfix(NTopBarRoomIcon __instance)
    //     {
    //         var runState = Traverse.Create(__instance).Field("_runState").GetValue();
    //         if (runState == null)
    //             return;
    //
    //         var currentMapPoint = Traverse.Create(runState).Property("CurrentMapPoint").GetValue();
    //         if (currentMapPoint == null)
    //             return;
    //
    //         var pointType = Traverse.Create(currentMapPoint).Property("PointType").GetValue<MapPointType>();
    //         if (pointType != MapPointType.Ancient)
    //         {
    //             Reset();
    //             return;
    //         }
    //         
    //         var act = Traverse.Create(runState).Property("Act").GetValue<ActModel>();
    //         var ancient = act?.Ancient;
    //         if (ancient == null)
    //             return;
    //
    //         string ancientEntry = ancient.Id.Entry;
    //         if (!AncientSpeakerRegistry.HasProfiles(ancientEntry))
    //         {
    //             Reset();   
    //             return;
    //         }
    //
    //         var icon = Traverse.Create(__instance).Field("_roomIcon").GetValue<TextureRect>();
    //         if (icon?.Texture == null)
    //             return;
    //
    //         var tex = MergeAllVariants(ancientEntry, icon.Texture,
    //             p => PreloadManager.Cache.GetCompressedTexture2D(p.IconPath));
    //
    //         icon.Texture = tex;
    //         ResetIconSize(icon, tex);
    //
    //         void Reset()
    //         {
    //             var icon2 = Traverse.Create(__instance).Field("_roomIcon").GetValue<TextureRect>();
    //             ResetIconSize(icon2, icon2.Texture);
    //         }
    //     }
    // }

}
