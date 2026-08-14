using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace TouhouAncients.Scripts.encounters;

/// <summary>
/// 东方角色挑战战斗的 Encounter 基类，统一处理自定义 BGM。
///
/// 子类只需重写 <see cref="BgmFileName"/> 指定自己的 BGM 文件，战斗开始/结束时由
/// <see cref="EncounterBgm"/>（scripts/EncounterBgm.cs）自动播放/停止——Encounter
/// 本身不接收战斗 Hook，故由全局订阅 CombatManager 事件的 EncounterBgm 统一处理。
/// 若同时重写 <see cref="BgmDisplayName"/>，<see cref="TouhouAncientEncounterBgmNamePatch"/>
/// 会在进入战斗后于屏幕底部显示该曲名。
/// </summary>
public abstract class TouhouAncientEncounter : CustomEncounterModel
{
    protected TouhouAncientEncounter(RoomType roomType) : base(roomType)
    {
    }

    /// <summary>
    /// 是否为挑战战斗（由 Ancient 事件的挑战选项进入）。挑战战斗不生成默认战斗奖励
    /// （金币/卡牌/药水），奖励只含 StartChallenge 传入 CombatRoom.ExtraRewards 的挑战遗物。
    /// 由 <see cref="TouhouAncientBase.StartChallenge"/> 在 canonical 实例上置位，游戏内部
    /// ToMutable（MemberwiseClone 拷贝字段）克隆战斗实例时继承该标志；调用返回后
    /// StartChallenge 会在 finally 中复位 canonical，故该标志只标记"这一场"经挑战进入的战斗。
    /// </summary>
    public bool IsChallenge { get; set; }

    /// <summary>
    /// 自定义 BGM 文件名（位于 res://debug_audio/，也可填完整的 res:// 路径）。
    /// 返回 null 表示不播放自定义 BGM。注意：对应音频文件的 import 需开启 loop=true 才能循环。
    /// </summary>
    public virtual string? BgmFileName => null;

    /// <summary>
    /// 战斗画面底部显示的 BGM 曲名。硬编码在各 Encounter 子类中，返回 null 则不显示。
    /// </summary>
    public virtual string? BgmDisplayName => null;

    /// <summary>
    /// 挑战战斗不生成默认战斗奖励（金币/卡牌/药水）：通过游戏原生 ModifyRewards 钩子
    /// （在 <see cref="Entry.Init"/> 用 ModHelper.SubscribeForRunStateHooks 把当前挑战
    /// Encounter 注册进 RunState Hook 监听者）在奖励生成时只保留 StartChallenge 传入
    /// CombatRoom.ExtraRewards 的挑战遗物。按类型判断（本基类只用于挑战战斗），读档重建后
    /// 依然生效，无需持久化 IsChallenge。正常/读档奖励流程（OfferRoomEndRewards →
    /// RewardsSet.GenerateWithoutOffering → Hook.ModifyRewards）都会调用。
    /// </summary>
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (room is not CombatRoom combatRoom) return false;
        if (!combatRoom.ExtraRewards.TryGetValue(player, out var challengeRewards)) return false;
        int before = rewards.Count;
        rewards.RemoveAll(r => !challengeRewards.Contains(r));
        return rewards.Count != before;
    }
}

/// <summary>
/// 进入正式战斗后，若当前 Encounter 是 <see cref="TouhouAncientEncounter"/> 且写了
/// <see cref="TouhouAncientEncounter.BgmDisplayName"/>，把
/// <c>res://scenes/ui/bgm_name_display.tscn</c> 挂到 <see cref="NCombatRoom"/> 上显示曲名。
/// 胜负都会触发 <see cref="CombatManager.CombatEnded"/>，此时立刻摘掉标签
/// （战斗房间在发奖/离开前仍会留着，不能等房间销毁）。
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
public static class TouhouAncientEncounterBgmNamePatch
{
    private const string DisplayNodeName = "TouhouBgmNameDisplay";
    private const string ScenePath = "res://scenes/ui/bgm_name_display.tscn";

    private static readonly FieldInfo VisualsField =
        AccessTools.Field(typeof(NCombatRoom), "_visuals");

    private static Control? _display;
    private static bool _subscribed;

    static void Postfix(NCombatRoom __instance)
    {
        if (__instance.Mode != CombatRoomMode.ActiveCombat) return;
        if (__instance.GetNodeOrNull(DisplayNodeName) != null) return;

        ICombatRoomVisuals? visuals = VisualsField?.GetValue(__instance) as ICombatRoomVisuals;
        if (visuals?.Encounter is not TouhouAncientEncounter encounter) return;
        if (string.IsNullOrEmpty(encounter.BgmDisplayName)) return;

        PackedScene? scene = GD.Load<PackedScene>(ScenePath);
        if (scene == null) return;

        Control display = scene.Instantiate<Control>();
        display.Name = DisplayNodeName;
        MegaLabel? label = display.GetNodeOrNull<MegaLabel>("%Label");
        if (label != null)
        {
            label.Text = "♪ " + encounter.BgmDisplayName;
        }
        __instance.AddChildSafely(display);
        _display = display;
        SubscribeCombatEnded();
    }

    private static void SubscribeCombatEnded()
    {
        if (_subscribed) return;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
        _subscribed = true;
    }

    private static void OnCombatEnded(CombatRoom room)
    {
        Hide();
    }

    private static void Hide()
    {
        if (_display != null && GodotObject.IsInstanceValid(_display))
        {
            _display.QueueFreeSafely();
        }
        _display = null;

        Node? leftover = NCombatRoom.Instance?.GetNodeOrNull(DisplayNodeName);
        leftover?.QueueFreeSafely();
    }
}

/// <summary>
/// 仅修正 <see cref="TouhouAncientEncounter"/> 挑战战斗的死亡文本。
/// 原版用最后一个地图点的 <c>Rooms.First()</c> 记成事件死亡，先古之民没有
/// <c>event.loss</c>，会落到 <c>MAP_POINT_HISTORY.debug</c>。
/// 不改重拳出击、历战假人等原版事件（它们继续走 <c>event.loss</c>）。
/// </summary>
public static class TouhouAncientEncounterDeathQuotePatch
{
    [HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
    public static class CreateRunHistoryEntry_Patch
    {
        static void Postfix(bool victory, bool isAbandoned)
        {
            if (victory || isAbandoned) return;
            if (RunManager.Instance.State?.CurrentRoom is not CombatRoom combatRoom) return;
            if (combatRoom.Encounter is not TouhouAncientEncounter) return;

            ModelId encounterId = combatRoom.Encounter.Id;
            RunHistory? history = RunManager.Instance.History;
            if (history == null) return;
            if (history.KilledByEncounter == encounterId && history.KilledByEvent == ModelId.none)
            {
                return;
            }

            RunHistory corrected = new RunHistory
            {
                SchemaVersion = history.SchemaVersion,
                PlatformType = history.PlatformType,
                GameMode = history.GameMode,
                Win = history.Win,
                Seed = history.Seed,
                StartTime = history.StartTime,
                RunTime = history.RunTime,
                Ascension = history.Ascension,
                BuildId = history.BuildId,
                WasAbandoned = history.WasAbandoned,
                KilledByEncounter = encounterId,
                KilledByEvent = ModelId.none,
                Players = history.Players,
                Acts = history.Acts,
                Modifiers = history.Modifiers,
                MapPointHistory = history.MapPointHistory
            };
            SaveManager.Instance.SaveRunHistory(corrected);
            if (RunManager.Instance.IsInProgress)
            {
                RunManager.Instance.History = corrected;
            }
        }
    }

    [HarmonyPatch(typeof(NRunHistory), nameof(NRunHistory.GetDeathQuote))]
    public static class GetDeathQuote_Patch
    {
        static void Postfix(ModelId characterId, ref string __result)
        {
            if (RunManager.Instance.State?.CurrentRoom is not CombatRoom combatRoom) return;
            if (combatRoom.Encounter is not TouhouAncientEncounter encounter) return;

            CharacterModel character = SaveUtil.CharacterOrDeprecated(characterId);
            LocString loss = encounter.GetLossMessageFor(character);
            StringBuilder text = new StringBuilder();
            text.Append(new LocString("game_over_screen", "ENCOUNTER_QUOTE_LEFT").GetRawText());
            text.Append(loss.GetFormattedText());
            text.Append(new LocString("game_over_screen", "ENCOUNTER_QUOTE_RIGHT").GetRawText());
            __result = text.ToString();
        }
    }
}
