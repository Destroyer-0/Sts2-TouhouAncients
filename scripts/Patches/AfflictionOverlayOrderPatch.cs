using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TouhouAncients.Scripts.Afflictions;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 把「魔力躁动」的侵蚀 overlay 提到整张卡之上。
/// 原版 card.tscn 里 OverlayContainer 排在 TitleBanner/TitleLabel/TypePlaque/EnergyIcon/Enchantment
/// 之前，overlay 只盖住卡框；魔力躁动要整张牌抖动，必须把容器挪到 CardContainer 末尾，
/// 侵蚀消失后再挪回原索引（原版移除 overlay 依赖 OverlayContainer 仍是它的父节点）。
/// 「叛乱之潮」不再用 overlay —— 卡图倒转见下方 <see cref="AfflictionPortraitFlipPatch"/>。
/// </summary>
[HarmonyPatch]
public static class AfflictionOverlayOrderPatch
{
    /// <summary>首次接触时记录的原版索引；之后不再更新（挪动过就不再是原版位置）。
    /// 用 StrongBox 包一层：ConditionalWeakTable 的 TValue 必须是引用类型。</summary>
    private static readonly ConditionalWeakTable<NCard, StrongBox<int>> OriginalIndices = new();

    [HarmonyPatch(typeof(NCard), "ReloadOverlay")]
    [HarmonyPostfix]
    private static void ReloadOverlayPostfix(NCard __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance)) return;

        Control? body = __instance.Body;
        Node? container = __instance.OverlayContainer;
        if (body == null || container == null) return;
        if (!GodotObject.IsInstanceValid(body) || !GodotObject.IsInstanceValid(container)) return;
        if (container.GetParent() != body) return; // 结构不符就别动

        int count = body.GetChildCount();
        if (count == 0) return;

        int originalIndex = OriginalIndices.GetValue(
            __instance,
            card => new StrongBox<int>(card.Body.GetChildren().IndexOf(card.OverlayContainer))).Value;

        // 视觉要盖住整张牌的侵蚀：只有魔力躁动（未来新增同类侵蚀时一并加到这里）
        bool onTop = __instance.Model?.Affliction is ManaRestlessness;
        int target = onTop ? count - 1 : Mathf.Clamp(originalIndex, 0, count - 1);

        if (container.GetIndex() != target)
        {
            body.MoveChildSafely(container, onTop ? -1 : target);
        }
    }
}

/// <summary>
/// 「叛乱之潮」的卡图倒转：直接把 <c>%Portrait</c> 节点绕自身中心转 180°。
/// 卡框在卡图之后绘制，会自然裁掉旋转后的边缘，不需要屏幕回读，也没有边缘伪影。
/// </summary>
[HarmonyPatch]
public static class AfflictionPortraitFlipPatch
{
    /// <summary>只记录本补丁改过的卡，避免误清游戏自身设置过的旋转。</summary>
    private static readonly ConditionalWeakTable<NCard, object> Flipped = new();

    [HarmonyPatch(typeof(NCard), "ReloadOverlay")]
    [HarmonyPostfix]
    private static void ReloadOverlayPostfix(NCard __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance)) return;
        if (__instance.GetNodeOrNull<TextureRect>("%Portrait") is not { } portrait) return;

        if (__instance.Model?.Affliction is RebellionTide)
        {
            Flipped.GetValue(__instance, _ => new object());
            portrait.Rotation = Mathf.Pi; // pivot 就是卡图中心，原地翻转
        }
        else if (Flipped.Remove(__instance))
        {
            portrait.Rotation = 0f;
        }
    }
}
