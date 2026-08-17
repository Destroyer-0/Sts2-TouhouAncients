using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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

    [CustomEnum("TouhouAncientSinkToBottom")]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword TouhouAncientSinkToBottom;

    [CustomEnum("TouhouAncientPuzzle")]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword TouhouAncientPuzzle;

}

/*
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
*/

/// <summary>
/// 无意识关键词单例：带有无意识的牌不能手动打出，但可以自动打出。
/// </summary>
public class KoishiUnplayableSingleton : CustomSingletonModel
{
    public KoishiUnplayableSingleton() : base(HookType.Combat)
    {
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Keywords.Contains(TouhouAncientKeywords.TouhouAncientKoishiUnplayable) && autoPlayType == AutoPlayType.None)
        {
            return false;
        }
        return true;
    }
}

/// <summary>
/// 沉底关键词单例：战斗开始时，将带有沉底关键词的牌移到抽牌堆底部。
/// </summary>
public class SinkToBottomSingleton : CustomSingletonModel
{
    public SinkToBottomSingleton() : base(HookType.Combat)
    {
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        await base.BeforeHandDraw(player, choiceContext, combatState);
        if (player.PlayerCombatState.TurnNumber != 1)
            return;

        CardPile pile = PileType.Draw.GetPile(player);
        var sinkCards = pile.Cards
            .Where(c => c.Keywords.Contains(TouhouAncientKeywords.TouhouAncientSinkToBottom))
            .ToList();
        foreach (CardModel card in sinkCards)
        {
            pile.MoveToBottomInternal(card);
        }
    }
}
