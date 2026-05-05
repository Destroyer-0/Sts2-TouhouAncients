using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using TouhouAncients.scripts.Keywords;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 为 Snakebite 卡牌添加 TouhouAncientSnakeBite 标签。
/// </summary>
[HarmonyPatch]
public static class SnakebitePatches
{
    [HarmonyPatch(typeof(Snakebite), ".ctor")]
    [HarmonyPostfix]
    private static void AddSnakeBiteTag(Snakebite __instance)
    {
        var tagsField = AccessTools.Field(typeof(CardModel), "_tags");
        if (tagsField == null) return;

        var tags = tagsField.GetValue(__instance) as HashSet<CardTag>;
        if (tags == null)
        {
            tags = new HashSet<CardTag>();
            tagsField.SetValue(__instance, tags);
        }
        tags.Add(TouhouAncientCardTags.TouhouAncientSnakeBite);
    }
}
