using System.Collections.Generic;
using System.Linq;
using BaseLib.Patches.Localization;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 「命运」标记的卡面呈现：额外文本与额外悬停提示。
///
/// - 额外文本通过 BaseLib 提供的 <see cref="DescriptionOverrides.CustomizeDescription"/>（前置事件）
///   追加到卡牌的基础描述末尾。原版的额外卡牌文本通道 <c>DynamicExtraCardText</c> 只服务于
///   附魔/苦恼，不对外开放；BaseLib 的 <c>DescriptionOverrides</c> 就是官方为
///   「想做关键词式效果、但不想真的占一个关键词」的场景预留的入口。
///   用前置而非后置事件，是为了让排布与附魔的额外文本一致：
///   <c>[前置关键词]\n基础描述\n额外文本\n[后置关键词]</c>。
/// - 额外悬停提示通过 Postfix <see cref="CardModel.HoverTips"/> getter 追加。
///
/// 两者都通过玩家反查命运吊坠实例，与流光共用同一份标记状态。
/// </summary>
[HarmonyPatch]
public static class FateMarkPatch
{
    /// <summary>本地化表名（命运相关文本追加在已有的 relics 表中，不新建表）。</summary>
    private const string LocTable = "relics";

    /// <summary>「命运」标题本地化 key。</summary>
    private const string FateTitleKey = "TOUHOUANCIENTS-FATE_PENDANT.fateTitle";

    /// <summary>「命运」描述本地化 key（用于 HoverTip 正文）。</summary>
    private const string FateDescriptionKey = "TOUHOUANCIENTS-FATE_PENDANT.fateDescription";

    /// <summary>卡面额外文本本地化 key。</summary>
    private const string FateExtraTextKey = "TOUHOUANCIENTS-FATE_PENDANT.fateExtraText";

    /// <summary>是否已订阅描述修改事件（幂等，避免重复订阅）。</summary>
    private static bool _subscribed;

    /// <summary>
    /// 订阅 BaseLib 的描述修改事件。由 <c>Entry.Init</c> 显式调用，
    /// 不依赖静态构造函数（HarmonyPatch 不会触发该类的静态构造）。
    /// </summary>
    public static void Initialize()
    {
        if (_subscribed) return;
        _subscribed = true;

        // 用前置事件：直接注入基础描述字符串，使额外文本落在
        // 「基础描述之后、后置关键词之前」，与附魔/苦恼的额外卡牌文本位置一致。
        DescriptionOverrides.CustomizeDescription += AppendFateExtraText;
    }

    /// <summary>取该牌拥有者身上的命运吊坠；没有遗物或未被标记时返回 null。</summary>
    private static FatePendant? GetMarkingRelic(CardModel card)
    {
        // 统一走 FateMarkOverlayPatch.IsFateMarked：它内部已处理 canonical 实例与
        // 「没有遗物 / 未标记」的情况，避免两处判定逻辑漂移。
        if (!FateMarkOverlayPatch.IsFateMarked(card)) return null;

        return card.Owner.GetRelic<FatePendant>();
    }

    /// <summary>
    /// 为被标记为「命运」的卡牌追加额外文本。
    /// 与附魔/苦恼的额外卡牌文本保持一致的紫色样式与排布位置。
    /// </summary>
    private static void AppendFateExtraText(CardModel card, Creature? target, ref string description)
    {
        if (GetMarkingRelic(card) == null) return;

        string extra = new LocString(LocTable, FateExtraTextKey).GetFormattedText();
        if (string.IsNullOrEmpty(extra)) return;

        // 与附魔一致：追加到基础描述末尾，且不参与卡片动态变量的渲染。
        description += $"\n[purple]{extra}[/purple]";
    }

    /// <summary>
    /// 为被标记为「命运」的卡牌追加一条悬停提示（标题「命运」+ 说明）。
    /// </summary>
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
    [HarmonyPostfix]
    private static void HoverTipsPostfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        FatePendant? relic = GetMarkingRelic(__instance);
        if (relic == null) return;

        var title = new LocString(LocTable, FateTitleKey);
        var description = new LocString(LocTable, FateDescriptionKey);
        // 注入遗物的动态变量，使 {Energy} 之类的占位符能正确渲染
        relic.DynamicVars.AddTo(description);

        __result = __result.Concat([new HoverTip(title, description)]);
    }
}
