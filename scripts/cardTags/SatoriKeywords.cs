using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.cardTags;

public static class SatoriKeywords
{
    /// <summary>
    /// 预见：检视你抽牌堆顶部的X张牌。你可以选择丢弃其中的任意张。
    /// 使用 [CustomEnum] 注册到 CardKeyword 枚举中。
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword SatoriScry;

    public static bool IsScry(this CardModel card)
    {
        return card.Keywords.Contains(SatoriScry);
    }
}
