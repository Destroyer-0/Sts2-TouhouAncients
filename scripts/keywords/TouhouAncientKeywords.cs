using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.cardTags;

public class TouhouAncientKeywords
{
    /// <summary>
    /// 使用 [CustomEnum] 注册到 CardKeyword 枚举中。
    /// </summary>
    [CustomEnum("SatoriScry")]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword SatoriScry;

    
    [CustomEnum("YinYangTranslation")]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword YinYangTranslation;

    public static bool IsScry(CardModel card)
    {
        return card.Keywords.Contains(SatoriScry);
    }
}
