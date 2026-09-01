using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    /// 处于"卡牌预览"上下文中的卡牌模板实例栈（<see cref="HoverTipFactory.FromCard"/> 调用压栈，
    /// HoverTips 求值匹配后弹栈）。
    /// 使用 [ThreadStatic] 保证多人/异步环境下各线程互不干扰。
    /// 用栈而非布尔值：HoverTips 求值过程中可能嵌套调用 FromCard（卡内嵌卡的预览），
    /// 栈能精确匹配"当前正在生成预览的卡牌模板实例"。
    /// </summary>
    [ThreadStatic]
    private static Stack<CardModel> _previewCards;

    /// <summary>惰性初始化线程本地栈（[ThreadStatic] 字段的初始化器不会在每个线程执行）。</summary>
    private static Stack<CardModel> PreviewCards => _previewCards ??= new Stack<CardModel>();

    /// <summary>进入卡牌预览上下文（<see cref="HoverTipFactory.FromCard"/> 的 Postfix 调用，压入被预览的卡牌模板实例）。</summary>
    internal static void EnterDerivedPreviewContext(CardModel card)
    {
        if (card != null)
        {
            PreviewCards.Push(card);
        }
    }

    /// <summary>
    /// 判断指定实例是否正处于"卡牌预览"上下文中（栈顶实例与它引用相等，且为 canonical 模板实例）。
    /// 匹配成功后弹栈（一次性消费），保证栈不残留、后续图鉴等场景悬停不受影响。
    /// </summary>
    private static bool IsDerivedPreview(CardModel instance)
    {
        var stack = _previewCards;
        if (stack == null || stack.Count == 0) return false;

        // 只匹配 canonical（不可变模板）实例：真实手牌/牌组卡是 mutable，绝不会误判
        if (stack.Peek() is { } top && ReferenceEquals(top, instance) && instance.IsCanonical)
        {
            stack.Pop();
            return true;
        }
        return false;
    }

    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        // 卡牌预览（FromCard 生成）不显示绘师
        if (IsDerivedPreview(__instance)) return;
        if (__instance.IsInCombat) return;

        // 只有东方卡牌且设置了绘师才显示
        if (__instance is not TouhouAncientCards { Author: { Length: > 0 } author }) return;

        var title = new LocString(LocTable, AuthorTitleKey);
        __result = new IHoverTip[] { new HoverTip(title, author) }.Concat(__result);
    }
}
/// <summary>
/// Patch <see cref="HoverTipFactory.FromCard(CardModel, bool)"/>：当该方法被调用生成卡牌预览时，
/// 将被预览的卡牌模板实例压栈，使 <see cref="CardAuthorHoverTipPatch"/> 在随后的 HoverTips 求值中
/// 识别出"衍生卡预览"场景并跳过绘师。
///
/// 注意：<see cref="HoverTipFactory.FromCardWithCardHoverTips{T}"/> 内部会先调用 FromCard 生成预览，
/// 再对同一张卡（ModelDb.Card&lt;T&gt;() 的 canonical 实例）求值 HoverTips，二者在同一线程内
/// 顺序同步执行，因此压栈后立即匹配弹栈，标记能准确覆盖求值窗口，且不会残留影响图鉴等场景。
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
        CardAuthorHoverTipPatch.EnterDerivedPreviewContext(card);
    }
}
