using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Rewards;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 解除原版「卡牌奖励替代选项最多 2 个」的限制：
/// <see cref="CardRewardAlternative.Generate"/> 超过 2 个会直接抛异常，UI 那边又是单行 HBoxContainer。
/// 这里去掉上限，并把容器换成会自动换行的 HFlowContainer。
/// </summary>
[HarmonyPatch]
public static class CardRewardAlternativesPatch
{
    // card_reward_alternative_button.tscn 的 custom_minimum_size
    private const float ButtonWidth = 276f;
    private const float ButtonHeight = 73f;
    private const int HSeparation = 75;
    private const int VSeparation = 10;

    private static readonly ConditionalWeakTable<CardReward, List<CardRewardAlternative>> CachedAlternatives = new();

    /// <summary>
    /// 原版在 Generate 末尾对 >2 个直接抛异常，导致奖励点不掉。重实现生成逻辑但去掉上限。
    /// </summary>
    [HarmonyPatch(typeof(CardRewardAlternative), nameof(CardRewardAlternative.Generate))]
    [HarmonyPrefix]
    private static bool GeneratePrefix(CardReward cardReward, ref IReadOnlyList<CardRewardAlternative> __result)
    {
        // 必须复用同一个 List 实例：CardReward.OnSelect 在循环外只取一次结果，之后一直按下标取选项。
        // 重掷会再调一次 Generate，若返回新实例，OnSelect 手里那份旧列表少了「重掷」，下标整体错位。
        List<CardRewardAlternative> list = CachedAlternatives.GetOrCreateValue(cardReward);
        list.Clear();
        if (cardReward.CanSkip)
        {
            list.Add(new CardRewardAlternative("Skip", PostAlternateCardRewardAction.EndSelectionAndDoNotCompleteReward));
        }
        if (cardReward.CanReroll)
        {
            list.Add(new CardRewardAlternative("REROLL", () =>
            {
                cardReward.Reroll();
                return Task.CompletedTask;
            }, PostAlternateCardRewardAction.DoNothing));
        }
        Hook.ModifyCardRewardAlternatives(cardReward.Player.RunState, cardReward.Player, cardReward, list);
        __result = list;
        return false;
    }

    /// <summary>
    /// 单行 HBox 换成 HFlow；原版按节点名取容器，替换后其余逻辑（清空/动画）无需改动。
    /// </summary>
    [HarmonyPatch(typeof(NCardRewardSelectionScreen), "_Ready")]
    [HarmonyPrefix]
    private static void ReadyPrefix(NCardRewardSelectionScreen __instance)
    {
        if (__instance.GetNodeOrNull<Control>("UI/RewardAlternatives") is not HBoxContainer old) return;
        Node? parent = old.GetParent();
        if (parent == null) return;

        HFlowContainer flow = new()
        {
            Name = old.Name,
            MouseFilter = old.MouseFilter,
            AnchorLeft = old.AnchorLeft,
            AnchorTop = old.AnchorTop,
            AnchorRight = old.AnchorRight,
            AnchorBottom = old.AnchorBottom,
            OffsetLeft = old.OffsetLeft,
            OffsetTop = old.OffsetTop,
            OffsetRight = old.OffsetRight,
            OffsetBottom = old.OffsetBottom,
            GrowHorizontal = old.GrowHorizontal,
            GrowVertical = Control.GrowDirection.End, // 新行向下长，顶边不动
            Alignment = FlowContainer.AlignmentMode.Center,
        };
        flow.AddThemeConstantOverride("h_separation", HSeparation);
        flow.AddThemeConstantOverride("v_separation", VSeparation);

        int index = old.GetIndex();
        old.Name = old.Name + "_Replaced";
        old.QueueFree();
        parent.AddChild(flow);
        parent.MoveChild(flow, index);
        // 窗口尺寸变化后按新宽度重新算行数
        flow.Resized += () => Relayout(flow);
    }

    [HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.RefreshOptions))]
    [HarmonyPostfix]
    private static void RefreshOptionsPostfix(NCardRewardSelectionScreen __instance)
    {
        if (__instance.GetNodeOrNull<HFlowContainer>("UI/RewardAlternatives") is { } flow)
        {
            Relayout(flow);
        }
    }

    /// <summary>按宽度算每行几个 → 行数 → 撑高容器（顶边保持不动）。</summary>
    private static void Relayout(HFlowContainer flow)
    {
        if (!GodotObject.IsInstanceValid(flow)) return;

        int count = 0;
        foreach (Node child in flow.GetChildren())
        {
            if (child is NCardRewardAlternativeButton button && !button.IsQueuedForDeletion())
            {
                count++;
            }
        }
        if (count == 0) return;

        int perRow = Mathf.Max(1, Mathf.FloorToInt((flow.Size.X + HSeparation + 0.5f) / (ButtonWidth + HSeparation)));
        int rows = (count + perRow - 1) / perRow;
        flow.OffsetBottom = flow.OffsetTop + rows * ButtonHeight + (rows - 1) * VSeparation;
    }
}
