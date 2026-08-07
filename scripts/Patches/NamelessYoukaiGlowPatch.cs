using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 未名妖魔发光提示：当玩家拥有活跃的未名妖魔 Power 时，
/// 手牌中与"上一张打出的牌"颜色（卡牌池）不同的牌显示红蓝流光，
/// 提示打出它们会触发未名妖魔的效果（获得1能量与本回合力量）。
///
/// 实现：
/// 1. Patch CardModel.ShouldGlowGold：让符合条件的牌进入金色高亮分支（原版 UI 依赖它决定 AnimShow）。
/// 2. Patch NHandCardHolder.UpdateCard：把符合条件卡牌的高亮材质替换为自定义红蓝流光 shader，
///    其余卡牌恢复默认材质。
/// </summary>
public static class NamelessYoukaiGlowPatch
{
    /// <summary>NCardHighlight._shaderMaterial 私有字段（AnimShow/AnimHide 的 tween 操作它）</summary>
    private static readonly FieldInfo ShaderMaterialField =
        typeof(NCardHighlight).GetField("_shaderMaterial", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>每张卡高亮节点缓存的默认材质（resource_local_to_scene = true，每实例独立副本）</summary>
    private static readonly ConditionalWeakTable<NCardHighlight, ShaderMaterial> DefaultMaterials = new();

    /// <summary>每张卡高亮节点缓存的红蓝流光材质（独立实例，避免 width 参数互相干扰）</summary>
    private static readonly ConditionalWeakTable<NCardHighlight, ShaderMaterial> FlowMaterials = new();

    private static Shader? _flowShader;

    /// <summary>
    /// 判断卡牌是否满足未名妖魔发光条件：
    /// 玩家有活跃的未名妖魔 Power，且该牌与"上一张打出的牌"颜色（卡牌池）不同。
    /// </summary>
    public static bool IsNamelessYoukaiEligible(CardModel? card)
    {
        if (card == null) return false;
        if (card.Owner?.Creature == null) return false;

        NamelessYoukaiPower? power = card.Owner.Creature.GetPowerInstances<NamelessYoukaiPower>().FirstOrDefault();
        if (power == null) return false;

        CardModel? previous = power.PreviousCard;
        if (previous == null) return false;
        if (previous == card) return false;
        // 与上一张打出的牌颜色（卡牌池）不同 → 发光
        return card.Pool != previous.Pool;
    }

    /// <summary>让符合条件的牌进入金色高亮分支（原版 UI 会因此调用 AnimShow 显示高亮）。</summary>
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldGlowGold), MethodType.Getter)]
    static void ShouldGlowGoldPostfix(CardModel __instance, ref bool __result)
    {
        if (__result) return;
        if (IsNamelessYoukaiEligible(__instance))
        {
            __result = true;
        }
    }

    /// <summary>
    /// 替换符合条件卡牌的高亮材质为红蓝流光材质；其余卡牌恢复默认材质。
    /// </summary>
    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard))]
    static void UpdateCardPostfix(NHandCardHolder __instance)
    {
        if (!__instance.IsNodeReady() || __instance.CardNode == null) return;
        CardModel? card = __instance.CardNode.Model;
        NCardHighlight? highlight = __instance.CardNode.CardHighlight;
        if (card == null || highlight == null) return;

        // 首次遇到时缓存默认材质（此时 Material 还是场景里的默认 card_ripple 材质）
        DefaultMaterials.GetValue(highlight, h => (ShaderMaterial)h.Material);

        bool shouldFlow = IsNamelessYoukaiEligible(card);
        if (shouldFlow)
        {
            ShaderMaterial? instance = GetFlowMaterialFor(highlight);
            if (instance == null) return; // shader 加载失败，退回默认发光
            ApplyMaterial(highlight, instance);
            // shader 内部自绘红蓝颜色，Modulate 恢复为白色
            highlight.Modulate = Colors.White;
            // 重新播放显示动画（作用于新的 _shaderMaterial）
            highlight.AnimShow();
        }
        else
        {
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
            }
        }
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
    /// 获取指定高亮节点的红蓝流光材质实例（每卡独立，避免 width 参数互相干扰）。
    /// 首次加载 shader；加载失败返回 null。
    /// </summary>
    private static ShaderMaterial? GetFlowMaterialFor(NCardHighlight highlight)
    {
        _flowShader ??= GD.Load<Shader>("res://shaders/nameless_youkai_flow.gdshader");
        if (_flowShader == null) return null;
        return FlowMaterials.GetValue(highlight, _ => new ShaderMaterial
        {
            Shader = _flowShader
        });
    }
}
