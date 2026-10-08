using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TouhouAncients.Scripts.Afflictions;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 把「魔力躁动 / 叛乱之潮」的侵蚀 overlay 提到整张卡之上。
/// 原版 card.tscn 里 OverlayContainer 排在 TitleBanner/TitleLabel/TypePlaque/EnergyIcon/Enchantment
/// 之前，overlay 只盖住卡框；这两个侵蚀要整张牌抖动/倒置，必须把容器挪到 CardContainer 末尾，
/// 侵蚀消失后再挪回原索引（原版移除 overlay 依赖 OverlayContainer 仍是它的父节点）。
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

        // 视觉要盖住整张牌的侵蚀：只有这两个（未来新增同类侵蚀时一并加到这里）
        bool onTop = __instance.Model?.Affliction is ManaRestlessness or RebellionTide;
        int target = onTop ? count - 1 : Mathf.Clamp(originalIndex, 0, count - 1);

        if (container.GetIndex() != target)
        {
            body.MoveChildSafely(container, onTop ? -1 : target);
        }
    }
}
