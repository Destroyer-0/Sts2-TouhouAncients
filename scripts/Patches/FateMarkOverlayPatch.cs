using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 「命运」标记的卡面视觉：从卡牌边缘向内漫延的血色雾气。
///
/// <para>
/// 视觉实现走原版侵蚀（affliction）的同款机制 —— 挂一个独立的 overlay 场景
/// 到 <c>NCard</c> 的 <c>OverlayContainer</c>，而不是替换卡牌本体的高亮材质。
/// 好处是与 <c>NCardHighlight.AnimShow/AnimHide</c> 的 tween 完全解耦，
/// 也不会盖掉原版「可打出＝金色高亮 / 不可打出＝不发光」的信息。
/// </para>
///
/// <para>
/// 原版的自动挂载入口 <c>NCard.ReloadOverlay</c> 只处理 <c>Model.Affliction</c> 与
/// <c>Model.HasBuiltInOverlay</c>，遗物自管的临时标记不在其列，
/// 因此这里用 Postfix 在它跑完后追加挂载/卸载自己的 overlay。
/// </para>
///
/// <para>
/// 判定入口是 <see cref="IsFateMarked"/>：从卡牌拥有者反查命运吊坠实例，
/// 与额外文本 / HoverTip 共用同一份标记状态，不需要额外同步。
/// </para>
/// </summary>
[HarmonyPatch]
public static class FateMarkOverlayPatch
{
    /// <summary>命运血雾 overlay 场景路径。</summary>
    private const string OverlayScenePath = "res://scenes/cards/overlays/fate_mark.tscn";

    /// <summary><c>NCard._overlayContainer</c>（原版存放 overlay 的全尺寸 Control）。</summary>
    private static readonly System.Reflection.FieldInfo OverlayContainerField =
        AccessTools.Field(typeof(NCard), "_overlayContainer");

    /// <summary>每个 NCard 节点对应的本系统 overlay 实例（节点销毁后自动回收）。</summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<NCard, Control> OurOverlays = new();

    /// <summary>该牌是否被其拥有者的命运吊坠标记为「命运」。</summary>
    public static bool IsFateMarked(CardModel? card)
    {
        // 卡牌预览（HoverTipFactory.FromCardWithCardHoverTips 等）会在 canonical 模板实例上
        // 求值，而 canonical 实例访问 Owner 会抛 CanonicalModelException。
        // 标记只可能存在于真实对局内的可变卡牌上。
        if (card == null || card.IsCanonical) return false;
        if (card.Owner is not { } owner) return false;
        return owner.GetRelic<FatePendant>()?.IsMarked(card) == true;
    }

    /// <summary>
    /// 在 <c>NCard.ReloadOverlay</c> 执行完之后，按「是否被标记为命运」增删本系统的 overlay。
    /// </summary>
    [HarmonyPatch(typeof(NCard), "ReloadOverlay")]
    [HarmonyPostfix]
    private static void ReloadOverlayPostfix(NCard __instance)
    {
        Sync(__instance);
    }

    /// <summary>
    /// 立即同步某张卡牌节点上的命运血雾 overlay（新增 / 移除）。
    /// 标记状态变化后由 <see cref="CardGlowPatch.RefreshHandCard"/> 触发。
    /// </summary>
    public static void Sync(NCard? cardNode)
    {
        if (cardNode == null) return;
        if (!GodotObject.IsInstanceValid(cardNode)) return;
        if (!cardNode.IsNodeReady()) return;

        CardModel? card = cardNode.Model;
        if (card == null) return;

        bool shouldShow = IsFateMarked(card);
        bool hasOverlay = OurOverlays.TryGetValue(cardNode, out Control? existing) &&
                          existing != null &&
                          GodotObject.IsInstanceValid(existing);

        if (shouldShow && !hasOverlay)
        {
            Mount(cardNode);
        }
        else if (!shouldShow && hasOverlay)
        {
            Unmount(cardNode);
        }
    }

    /// <summary>挂载血雾 overlay 到卡牌节点的 OverlayContainer。</summary>
    private static void Mount(NCard cardNode)
    {
        if (OverlayContainerField.GetValue(cardNode) is not Node container) return;
        if (!GodotObject.IsInstanceValid(container)) return;

        // 与原版 AfflictionModel.CreateOverlay 一致：走 PreloadManager 的资产缓存。
        // 直接用 GD.Load 会绕过预加载清单，在导出后的 PCK 里可能取不到资源。
        Control? overlay = PreloadManager.Cache.GetScene(OverlayScenePath)
            ?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
        if (overlay == null)
        {
            Log.Warn($"[TouhouAncients] 命运血雾 overlay 场景加载失败：{OverlayScenePath}");
            return;
        }

      
        overlay.MouseFilter = Control.MouseFilterEnum.Ignore;

        container.AddChild(overlay);
        OurOverlays.AddOrUpdate(cardNode, overlay);
    }

    /// <summary>移除本系统挂上去的血雾 overlay（只动自己的，不碰原版 overlay）。</summary>
    private static void Unmount(NCard cardNode)
    {
        if (!OurOverlays.TryGetValue(cardNode, out Control? overlay)) return;
        OurOverlays.Remove(cardNode);

        if (overlay == null || !GodotObject.IsInstanceValid(overlay)) return;
        overlay.GetParent()?.RemoveChild(overlay);
        overlay.QueueFree();
    }
}
