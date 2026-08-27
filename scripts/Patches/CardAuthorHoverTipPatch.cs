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
    /// 当前处于"衍生卡预览"上下文的嵌套深度（FromCardWithCardHoverTips 求值 HoverTips 期间大于 0）。
    /// 使用 [ThreadStatic] 保证多人/异步环境下各线程互不干扰。
    /// 用计数器而非布尔值：HoverTips 求值过程中可能嵌套调用 FromCardWithCardHoverTips，
    /// 计数器保证最外层退出后才关闭标记。
    /// </summary>
    [ThreadStatic]
    private static int _derivedPreviewDepth;

    /// <summary>进入衍生卡预览上下文（FromCardWithCardHoverTips 的 Prefix 调用）。</summary>
    internal static void EnterDerivedPreviewContext()
    {
        _derivedPreviewDepth++;
    }

    /// <summary>退出衍生卡预览上下文（FromCardWithCardHoverTips 的 Postfix 调用）。</summary>
    internal static void ExitDerivedPreviewContext()
    {
        if (_derivedPreviewDepth > 0)
        {
            _derivedPreviewDepth--;
        }
    }

    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        // 衍生卡预览（FromCardWithCardHoverTips）不显示绘师
        if (_derivedPreviewDepth > 0) return;

        // 只有东方卡牌且设置了绘师才显示
        if (__instance is not TouhouAncientCards { Author: { Length: > 0 } author }) return;

        var title = new LocString(LocTable, AuthorTitleKey);
        __result = new IHoverTip[] { new HoverTip(title, author) }.Concat(__result);
    }
}

/// <summary>
/// Patch <see cref="HoverTipFactory.FromCardWithCardHoverTips{T}"/>：在求值衍生卡 HoverTips 期间
/// 打开"衍生卡预览"标记，使绘师 HoverTip 不插入到衍生卡预览中。
///
/// 注意：FromCardWithCardHoverTips 内部对 ModelDb.Card&lt;T&gt;().HoverTips 的求值是同步发生的
/// （作为 Concat 的参数立即求值），因此 Prefix 打开标记、原方法体求值、Postfix 关闭标记
/// 在同一个线程内顺序执行，标记能准确覆盖求值窗口。
/// </summary>
[HarmonyPatch]
public static class CardAuthorDerivedPreviewPatch
{
    /// <summary>
    /// 通过 TargetMethod 精确定位泛型方法定义 FromCardWithCardHoverTips&lt;T&gt;。
    /// 泛型方法定义只有一份 IL，patch 后对所有泛型实例（T 为引用类型时共享代码）生效。
    /// </summary>
    private static System.Reflection.MethodBase TargetMethod()
    {
        return typeof(HoverTipFactory).GetMethods()
            .First(m => m.Name == nameof(HoverTipFactory.FromCardWithCardHoverTips) && m.IsGenericMethodDefinition);
    }

    [HarmonyPrefix]
    private static void Prefix()
    {
        CardAuthorHoverTipPatch.EnterDerivedPreviewContext();
    }

    [HarmonyPostfix]
    private static void Postfix()
    {
        CardAuthorHoverTipPatch.ExitDerivedPreviewContext();
    }
}
