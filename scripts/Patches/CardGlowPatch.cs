using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using TouhouAncients.Scripts.powers;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 一种卡牌流光样式。
///
/// 每个样式使用独立的 shader 文件，由 <see cref="ShaderPath"/> 指定；
/// <see cref="Predicate"/> 决定某张卡牌是否命中该样式。
/// 一个卡牌同时命中多个样式时，按注册顺序取第一个。
/// </summary>
public sealed class CardGlowStyle
{
    /// <summary>唯一标识，用于注销与材质缓存。</summary>
    public required string Id { get; init; }

    /// <summary>shader 资源路径（res:// 形式）。每个样式一个独立文件。</summary>
    public required string ShaderPath { get; init; }

    /// <summary>命中判定。注意本属性会在 UI 每帧刷新路径上被调用，应保持轻量。</summary>
    public required Func<CardModel, bool> Predicate { get; init; }

    /// <summary>惰性加载的 shader 实例（所有命中的卡牌共用同一个 Shader，材质各自独立）。</summary>
    internal Shader? Shader { get; set; }

    /// <summary>shader 是否已尝试加载过（避免加载失败时反复重试）。</summary>
    internal bool ShaderLoadAttempted { get; set; }
}

/// <summary>
/// 通用卡牌流光系统。
///
/// 各功能模块通过 <see cref="Register"/> 声明「什么卡牌该发光、用什么 shader 发光」，
/// 本类统一负责把它映射到原版 UI：
/// 1. Postfix <see cref="CardModel.ShouldGlowGold"/> getter —— 命中的牌进入原版高亮分支
///    （原版 UI 依赖它决定是否调用 <c>AnimShow</c>）。
/// 2. Postfix <see cref="NHandCardHolder.UpdateCard"/> —— 把命中卡牌的高亮材质替换为该样式的
///    流光材质；未命中的卡牌在本系统此前替换过时才还原默认材质（避免干扰原版金光与其它 mod）。
///
/// 注意：原版 <c>NHandCardHolder</c> 的高亮分支要求 <c>CardModel.CanPlay()</c>，
/// 而「命运」标记的牌即使在当前费用下不可打出也应当发光提示，
/// 因此 UpdateCard 的 Postfix 会主动调用 <c>AnimShow()</c> 覆盖原版的 <c>AnimHide()</c>。
/// </summary>
[HarmonyPatch]
public static class CardGlowPatch
{
    /// <summary>已注册的样式（保持注册顺序，先注册者优先）。</summary>
    private static readonly List<CardGlowStyle> Styles = [];

    /// <summary>按 Id 索引的样式（用于幂等注册与注销）。</summary>
    private static readonly Dictionary<string, CardGlowStyle> StylesById = new();

    /// <summary><c>NCardHighlight._shaderMaterial</c> 私有字段（AnimShow/AnimHide 的 tween 操作它）。</summary>
    private static readonly FieldInfo ShaderMaterialField =
        typeof(NCardHighlight).GetField("_shaderMaterial", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>每张卡高亮节点缓存的默认材质（resource_local_to_scene = true，每实例独立副本）。</summary>
    private static readonly ConditionalWeakTable<NCardHighlight, ShaderMaterial> DefaultMaterials = new();

    /// <summary>每张卡高亮节点、每个样式缓存的流光材质（独立实例，避免 shader 参数互相干扰）。</summary>
    private static readonly ConditionalWeakTable<NCardHighlight, Dictionary<string, ShaderMaterial>> FlowMaterials = new();

    /// <summary>
    /// 注册一个流光样式。同 Id 重复注册时覆盖旧样式（用于热重载或调试）。
    /// </summary>
    public static void Register(CardGlowStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        if (string.IsNullOrEmpty(style.Id)) throw new ArgumentException("样式 Id 不能为空。", nameof(style));

        if (StylesById.TryGetValue(style.Id, out CardGlowStyle? existing))
        {
            Styles.Remove(existing);
        }

        StylesById[style.Id] = style;
        Styles.Add(style);
    }

    /// <summary>
    /// 注销指定 Id 的样式。注销后不再有新卡牌命中该样式；
    /// 已被染色的节点会在下一次 UI 刷新时自动还原默认材质
    /// （还原判定基于「当前材质是否为本系统注册过的任一材质」，不依赖样式的存活状态）。
    /// </summary>
    public static bool Unregister(string id)
    {
        if (!StylesById.Remove(id, out CardGlowStyle? style)) return false;
        Styles.Remove(style);
        return true;
    }

    /// <summary>
    /// 取卡牌命中的第一个样式；未命中返回 null。
    /// 供 <see cref="CardModel.ShouldGlowGold"/> 与 <see cref="NHandCardHolder.UpdateCard"/> 的补丁共用。
    /// </summary>
    public static CardGlowStyle? GetStyleFor(CardModel? card)
    {
        if (card == null) return null;
        // 图鉴 / 卡牌预览会对 canonical 模板实例求值（如 HoverTipFactory.FromCardWithCardHoverTips），
        // 而 canonical 实例访问 Owner 会抛 CanonicalModelException，因此在入口统一挡掉。
        // 标记状态只可能存在于真实对局内的可变实例上，canonical 直接视为未命中。
        if (card.IsCanonical) return null;

        foreach (CardGlowStyle style in Styles)
        {
            if (style.Predicate(card)) return style;
        }
        return null;
    }

    /// <summary>
    /// 静态构造函数：注册本 Mod 内置的流光样式。
    ///
    /// 任何对 <see cref="CardGlowPatch"/> 静态成员的首次访问（<see cref="Register"/>、
    /// <see cref="GetStyleFor"/>，以及 Harmony 补丁内部的调用）都会触发本构造函数，
    /// 因此内置样式必定在第一次使用之前完成注册，不需要依赖 <c>Entry.Init</c> 的显式调用。
    /// </summary>
    static CardGlowPatch()
    {
        // 未名妖魔：手牌中与"上一张打出的牌"颜色（卡牌池）不同的牌显示红蓝流光。
        Register(new CardGlowStyle
        {
            Id = "TouhouAncients.NamelessYoukai",
            ShaderPath = "res://shaders/nameless_youkai_flow.gdshader",
            Predicate = IsNamelessYoukaiEligible
        });

        
    }

    /// <summary>
    /// 未名妖魔发光条件：玩家拥有活跃的未名妖魔 Power，
    /// 且该牌与"上一张打出的牌"颜色（卡牌池）不同。
    /// </summary>
    private static bool IsNamelessYoukaiEligible(CardModel card)
    {
        if (card.Owner?.Creature == null) return false;
        if (!card.CanPlay()) return false;

        NamelessYoukaiPower? power = card.Owner.Creature.GetPowerInstances<NamelessYoukaiPower>().FirstOrDefault();
        if (power == null) return false;

        CardModel? previous = power.PreviousCard;
        if (previous == null) return false;
        if (previous == card) return false;
        return card.Pool != previous.Pool;
    }

    /// <summary>
    /// 让命中的牌进入原版金色高亮分支（原版 UI 会因此调用 <c>AnimShow</c> 显示高亮）。
    /// </summary>
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldGlowGold), MethodType.Getter)]
    [HarmonyPostfix]
    private static void ShouldGlowGoldPostfix(CardModel __instance, ref bool __result)
    {
        if (__result) return;
        if (GetStyleFor(__instance) != null)
        {
            __result = true;
        }
    }

    /// <summary>
    /// 把命中卡牌的高亮材质替换为该样式的流光材质；未命中且此前被替换过时还原默认材质。
    /// </summary>
    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard))]
    [HarmonyPostfix]
    private static void UpdateCardPostfix(NHandCardHolder __instance)
    {
        if (!__instance.IsNodeReady() || __instance.CardNode == null) return;
        CardModel? card = __instance.CardNode.Model;
        NCardHighlight? highlight = __instance.CardNode.CardHighlight;
        if (card == null || highlight == null) return;

        // 首次遇到时缓存默认材质（此时 Material 还是场景里的默认 card_ripple 材质）
        DefaultMaterials.GetValue(highlight, h => (ShaderMaterial)h.Material);

        CardGlowStyle? style = GetStyleFor(card);
        if (style != null)
        {
            // 样式未变且材质已就位时不重复替换，避免打断正在播放的 tween
            if (IsFlowMaterialApplied(highlight, style)) return;

            ShaderMaterial? instance = GetFlowMaterialFor(highlight, style);
            if (instance == null) return; // shader 加载失败，退回原版发光
            ApplyMaterial(highlight, instance);
            // 重新播放显示动画（作用于新的 _shaderMaterial）；原版对不可打出的牌会调 AnimHide，
            // 这里主动 AnimShow 覆盖，使被标记的牌即使费用不足也保持发光。
            highlight.AnimShow();
            // shader 内部自绘颜色，Modulate 恢复为白色
            highlight.Modulate = Colors.White;
            return;
        }

        // 未命中：只有本系统此前真的替换过材质时才还原，
        // 从未被本系统动过的牌完全放手，避免清除原版金光或其它 mod 自定义的颜色/材质。
        if (!IsAnyFlowMaterialApplied(highlight)) return;

        if (DefaultMaterials.TryGetValue(highlight, out ShaderMaterial? defaultMaterial))
        {
            ApplyMaterial(highlight, defaultMaterial);
            // 恢复原版高亮颜色逻辑（与 UpdateCard 原逻辑一致）
            if (card.CanPlay() || card.ShouldGlowRed || card.ShouldGlowGold)
            {
                highlight.Modulate = NCardHighlight.playableColor;
                if (card.ShouldGlowRed) highlight.Modulate = NCardHighlight.red;
                else if (card.ShouldGlowGold) highlight.Modulate = NCardHighlight.gold;
            }
            else
            {
                highlight.AnimHide();
            }
        }
    }

    /// <summary>判断该高亮节点当前是否正使用指定样式的流光材质。</summary>
    private static bool IsFlowMaterialApplied(NCardHighlight highlight, CardGlowStyle style)
    {
        return FlowMaterials.TryGetValue(highlight, out Dictionary<string, ShaderMaterial>? materials) &&
               materials.TryGetValue(style.Id, out ShaderMaterial? material) &&
               ReferenceEquals(highlight.Material, material);
    }

    /// <summary>判断该高亮节点当前是否正使用本系统任意样式的流光材质。</summary>
    private static bool IsAnyFlowMaterialApplied(NCardHighlight highlight)
    {
        if (!FlowMaterials.TryGetValue(highlight, out Dictionary<string, ShaderMaterial>? materials)) return false;
        foreach (ShaderMaterial material in materials.Values)
        {
            if (ReferenceEquals(highlight.Material, material)) return true;
        }
        return false;
    }

    /// <summary>
    /// 替换高亮节点的 Material 并同步私有字段 _shaderMaterial，
    /// 使 AnimShow/AnimHide 的 tween（每帧调用 SetShaderParameter）作用于新材质。
    /// </summary>
    private static void ApplyMaterial(NCardHighlight highlight, ShaderMaterial material)
    {
        highlight.Material = material;
        ShaderMaterialField.SetValue(highlight, material);
    }

    /// <summary>
    /// 获取指定高亮节点、指定样式的流光材质实例（每卡每样式独立，避免 shader 参数互相干扰）。
    /// 首次加载 shader；加载失败返回 null。
    /// </summary>
    private static ShaderMaterial? GetFlowMaterialFor(NCardHighlight highlight, CardGlowStyle style)
    {
        if (!style.ShaderLoadAttempted)
        {
            style.Shader = GD.Load<Shader>(style.ShaderPath);
            style.ShaderLoadAttempted = true;
            if (style.Shader == null)
            {
                MegaCrit.Sts2.Core.Logging.Log.Warn($"[TouhouAncients] 卡牌流光 shader 加载失败：{style.ShaderPath}");
            }
        }
        if (style.Shader == null) return null;

        Dictionary<string, ShaderMaterial> materials =
            FlowMaterials.GetValue(highlight, _ => new Dictionary<string, ShaderMaterial>());
        if (!materials.TryGetValue(style.Id, out ShaderMaterial? material))
        {
            material = new ShaderMaterial { Shader = style.Shader };
            materials[style.Id] = material;
        }
        return material;
    }

    /// <summary>
    /// 立即刷新某张卡的卡面与视觉表现：标记状态变化后调用，
    /// 否则要等下一次手牌重排才会显示/隐藏高亮、血雾叠加层与额外文本。
    /// 按卡牌当前所在牌堆处理；不在场上（如牌组/已被移除）时静默跳过。
    /// </summary>
    public static void RefreshHandCard(CardModel? card)
    {
        if (card == null) return;
        if (!CombatManager.Instance.IsInProgress) return;

        PileType pileType = card.Pile?.Type ?? PileType.None;
        if (pileType == PileType.None) return;

        NCard? node = NCard.FindOnTable(card, pileType);
        if (node == null) return;

        // 手牌走 Holder 以便同时刷新高亮材质；其余牌堆只需刷新卡面文本
        if (node.GetParent() is NHandCardHolder holder) holder.UpdateCard();
        else node.UpdateVisuals(pileType, CardPreviewMode.Normal);

        // 命运血雾是独立叠加层，不随卡面刷新自动增删，需要单独同步
        FateMarkOverlayPatch.Sync(node);
    }
}
