using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.cardTags;

public class TouhouAncientKeywords
{
    /// <summary>
    /// 使用 [CustomEnum] 注册到 CardKeyword 枚举中。
    /// </summary>
    [CustomEnum("TouhouAncientSatoriScry")]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword TouhouAncientSatoriScry;
    
    [CustomEnum("TouhouAncientKoishiUnplayable")]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword TouhouAncientKoishiUnplayable;

    
    [CustomEnum("TouhouAncientYinYangTranslation")]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword TouhouAncientYinYangTranslation;
    
    [CustomEnum("TouhouAncientDonate")]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword TouhouAncientDonate;
    
    
    [CustomEnum("TouhouAncientRegent")]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword TouhouAncientRegent;
    
    [CustomEnum("TouhouAncientCherryBlossoms")]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword TouhouAncientCherryBlossoms;
    
    [CustomEnum("TouhouAncientFilth")]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword TouhouAncientFilth;
    

}

//Patch: 带有无意识的牌不能被玩家打出。
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
