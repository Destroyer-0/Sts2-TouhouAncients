using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;

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
            iconNode.GetNode<TextureRect>("Icon").Texture = GD.Load<Texture2D>(profile.IconPath);
            iconNode.GetNode<TextureRect>("Icon/Outline").Texture = GD.Load<Texture2D>(profile.OutlinePath);

            var dialogueColor = profile.DialogueColor;
            __instance.GetNode<Control>("%Bubble").SelfModulate = dialogueColor;
            __instance.GetNode<Control>("%DialogueTailLeft").SelfModulate = dialogueColor;
        }
    }

}
