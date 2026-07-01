using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace TouhouAncients.Scripts.cardTags;

[RegisterOwnedCardKeyword(nameof(TouhouAncientSatoriScry))]
[RegisterOwnedCardKeyword(nameof(TouhouAncientKoishiUnplayable))]
[RegisterOwnedCardKeyword(nameof(TouhouAncientYinYangTranslation))]
[RegisterOwnedCardKeyword(nameof(TouhouAncientDonate))]
[RegisterOwnedCardKeyword(nameof(TouhouAncientRegent))]
[RegisterOwnedCardKeyword(nameof(TouhouAncientCherryBlossoms))]
[RegisterOwnedCardKeyword(nameof(TouhouAncientFilth))]
public class TouhouAncientKeywords
{
    public static readonly CardKeyword TouhouAncientSatoriScry = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(TouhouAncientSatoriScry));
    public static readonly CardKeyword TouhouAncientKoishiUnplayable = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(TouhouAncientKoishiUnplayable));
    public static readonly CardKeyword TouhouAncientYinYangTranslation = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(TouhouAncientYinYangTranslation));
    public static readonly CardKeyword TouhouAncientDonate = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(TouhouAncientDonate));
    public static readonly CardKeyword TouhouAncientRegent = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(TouhouAncientRegent));
    public static readonly CardKeyword TouhouAncientCherryBlossoms = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(TouhouAncientCherryBlossoms));
    public static readonly CardKeyword TouhouAncientFilth = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(TouhouAncientFilth));
}

// Patch: 带有无意识的牌不能被玩家打出。
[HarmonyPatch(typeof(CardModel))]
public static class TouhouAncientKoishiUnplayablePatch
{
    [HarmonyPatch("get_IsPlayable")]
    [HarmonyPostfix]
    private static void ApplyKoishiUnplayable(CardModel __instance, ref bool __result)
    {
        if (__result && TouhouAncientCmd.IsKoishi(__instance))
        {
            __result = false;
        }
    }
}
