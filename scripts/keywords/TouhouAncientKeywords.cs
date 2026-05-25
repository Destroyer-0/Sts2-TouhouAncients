using BaseLib.Patches.Content;
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

    public static bool IsScry(CardModel card)
    {
        return card.Keywords.Contains(TouhouAncientSatoriScry);
    }
}
