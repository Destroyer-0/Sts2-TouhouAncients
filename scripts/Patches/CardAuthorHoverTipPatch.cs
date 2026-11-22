using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 卡牌绘师 HoverTip 补丁。
///
/// 为所有 <see cref="TouhouAncientCards"/>（且 Author 非空）的卡牌悬停提示在最前面追加一条
/// "绘师：XXX" 的 HoverTip。标题使用 card_keywords 表中的
/// TOUHOUANCIENTS-TOUHOUANCIENTAUTHOR.title（各语言本地化），描述直接使用 Author 字符串
/// （绘师名通常不需要翻译，保持原文）。
///
/// 同时排除"衍生卡预览"场景：<see cref="HoverTipFactory.FromCardWithCardHoverTips{T}"/> 生成的
/// 预览（遗物/事件/能力里展示的卡牌缩略预览）不显示绘师，只有真正的卡牌本体悬停才显示。
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
public static class CardAuthorHoverTipPatch
{
    /// <summary>本地化表名。</summary>
    private const string LocTable = "card_keywords";

    /// <summary>绘师标题本地化 key。</summary>
    private const string AuthorTitleKey = "TOUHOUANCIENTS-TOUHOUANCIENTAUTHOR.title";

    /// <summary>
    /// 待配对的预览卡（FromCard 登记、下一次 HoverTips 求值消费）。
    /// 不用栈：卡自身的 ExtraHoverTips 可能再调 FromCard，栈顶会被顶掉，外层配对失败且记录滞留。
    /// [ThreadStatic] 保证多人/异步下各线程互不干扰。
    /// </summary>
    [ThreadStatic]
    private static CardModel? _pendingPreview;

    /// <summary>登记预览卡（统一存 canonical：upgrade=true 时 FromCard 内部是克隆）。</summary>
    internal static void MarkPreview(CardModel card) => _pendingPreview = card.CanonicalInstance;

    [HarmonyPrefix]
    private static void Prefix(CardModel __instance, out bool __state)
    {
        // 无论命中与否都消费：调用方不求值 HoverTips 时，令牌不能残留到下一次
        var pending = _pendingPreview;
        _pendingPreview = null;
        // 只认 canonical：真实手牌/牌组卡是 mutable，绝不会误判
        __state = pending != null && __instance.IsCanonical && ReferenceEquals(pending, __instance);
    }

    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, bool __state, ref IEnumerable<IHoverTip> __result)
    {
        // 求值期间 ExtraHoverTips 新登记的令牌其 FromCard 已返回，必然配对不上，直接丢弃
        _pendingPreview = null;

        // 卡牌预览（FromCard 生成）不显示绘师
        if (__state) return;
        if (__instance.IsInCombat) return;

        // 只有东方卡牌且设置了绘师才显示
        if (__instance is not TouhouAncientCards { Author: { Length: > 0 } author }) return;

        var title = new LocString(LocTable, AuthorTitleKey);
        __result = new IHoverTip[] { new HoverTip(title, author) }.Concat(__result);
    }
}
/// <summary>
/// Patch <see cref="HoverTipFactory.FromCard(CardModel, bool)"/>：生成卡牌预览时登记令牌，
/// 使 <see cref="CardAuthorHoverTipPatch"/> 在随后的 HoverTips 求值中识别"衍生卡预览"并跳过绘师。
/// <see cref="HoverTipFactory.FromCardWithCardHoverTips{T}"/> 的 HoverTips 参数是立即求值的，
/// 登记后紧接消费，令牌不会影响图鉴等正常悬停。
/// </summary>
[HarmonyPatch]
public static class CardAuthorDerivedPreviewPatch
{
    /// <summary>
    /// 通过 TargetMethod 精确匹配非泛型重载 FromCard(CardModel, bool)。
    /// （HoverTipFactory 还有一个泛型重载 FromCard&lt;T&gt;(bool)，必须指定参数类型以免歧义。）
    /// </summary>
    private static System.Reflection.MethodBase TargetMethod()
    {
        return typeof(HoverTipFactory).GetMethod(
            nameof(HoverTipFactory.FromCard),
            new[] { typeof(CardModel), typeof(bool) });
    }

    [HarmonyPostfix]
    private static void Postfix(CardModel card)
    {
        CardAuthorHoverTipPatch.MarkPreview(card);
    }
}
