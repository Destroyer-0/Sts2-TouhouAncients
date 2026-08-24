using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace TouhouAncients.Scripts.encounters;

/// <summary>
/// 东方角色挑战战斗的 Encounter 基类，统一处理自定义 BGM。
///
/// 子类只需重写 <see cref="BgmFileName"/> 指定自己的 BGM 文件。默认战斗开始/结束时由
/// <see cref="EncounterBgm"/>（scripts/EncounterBgm.cs）自动播放/停止；
/// <see cref="AutoStartBgm"/> 为 false 时开场只静音原版音乐，稍后手动调用
/// <see cref="EncounterBgm.Start"/> 再播。Encounter 本身不接收战斗 Hook，故由全局订阅
/// CombatManager 事件的 EncounterBgm 统一处理。
/// 战斗底部曲名显示是通用功能：<see cref="TouhouAncientEncounterBgmNamePatch"/> 对
/// 任意 Encounter 生效，只要本地化表 encounters 存在 "{Id.Entry}.bgm" 键（如
/// TOUHOUANCIENTS-KIRISAME_MARISA_ENCOUNTER.bgm）就显示，键不存在则不显示。
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
    /// 战斗开始时是否立刻播放 <see cref="BgmFileName"/>。返回 false 时只静音原版 FMOD 音乐，
    /// 由怪物技能等手动调用 <see cref="EncounterBgm.Start"/> 再开始播放。
    /// </summary>
    public virtual bool AutoStartBgm => true;

    /// <summary>
    /// 专属战斗背景主场景路径（可为 null）。不为 null 时，战斗背景将被替换为
    /// 该 .tscn 场景（根节点需挂 <c>NCombatBackground</c> 脚本；单张图片背景可把
    /// <c>TextureRect</c> 作为其子节点承载）。返回 null 表示保留当前 Act 的默认背景。
    ///
    /// 实现说明：本属性由 <see cref="CustomEncounterBackground"/> 桥接给 BaseLib 内置的
    /// 自定义背景机制（<c>CustomEncounterModel</c> 已提供 GetBackgroundAssets /
    /// CreateBackgroundAssetsForCustom 的 Harmony 前缀钩子，自动填充并返回这里的资产），
    /// 因此无需再写任何 Patch。使用 <see cref="BaseLib.Utils.CustomBackgroundAssets"/>
    /// 的固定资产构造，且 BgLayers 为空、FgLayer 为 null，NCombatBackground 不会附加分层，
    /// 主场景即整个背景，最简单可控。
    /// </summary>
    public virtual string? CustomBackgroundScenePath => null;

    /// <summary>
    /// 把 <see cref="CustomBackgroundScenePath"/> 桥接为 BaseLib 的自定义背景资产。
    /// 返回 null 时 BaseLib 会退回 Act 默认背景。除个别需要按层随机拼装的场景外，
    /// 子类通常无需重写本方法，直接填 <see cref="CustomBackgroundScenePath"/> 即可。
    /// </summary>
    public override BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng)
    {
        string? path = CustomBackgroundScenePath;
        return string.IsNullOrEmpty(path) ? null : new CustomBackgroundAssets(path, [], null!);
    }

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
/// 进入正式战斗后，若本地化表 encounters 存在当前 Encounter 的 "{Id.Entry}.bgm" 键
/// （如 TOUHOUANCIENTS-KIRISAME_MARISA_ENCOUNTER.bgm），把
/// <c>res://scenes/ui/bgm_name_display.tscn</c> 挂到 <see cref="NCombatRoom"/> 上显示曲名。
/// 对任意 <see cref="EncounterModel"/> 生效（含原版与其他 mod 的 Encounter），键不存在则不显示。
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
        if (visuals?.Encounter is not EncounterModel encounter) return;
        string? bgmDisplayName = LocString.GetIfExists("encounters", encounter.Id.Entry + ".bgm")?.GetFormattedText();
        if (string.IsNullOrEmpty(bgmDisplayName)) return;

        PackedScene? scene = GD.Load<PackedScene>(ScenePath);
        if (scene == null) return;

        Control display = scene.Instantiate<Control>();
        display.Name = DisplayNodeName;
        Label? label = display.GetNodeOrNull<Label>("%Label");
        if (label != null)
        {
            label.Text = "♪ Bgm: " + bgmDisplayName;
        }
        __instance.AddChildSafely(display);
        _display = display;
        SubscribeCombatEnded();
    }

    
    public static void TryHide()
    {
        if (_display != null && _subscribed)
            _display.Hide();
    }
    
    public static void TryShow()
    {
        if (_display != null && _subscribed)
            _display.Show();
    }

    private static void SubscribeCombatEnded()
    {
        if (_subscribed) return;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
        _subscribed = true;
    }

    private static void OnCombatEnded(CombatRoom room)
    {
        End();
    }

    
    private static void End()
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

// /// <summary>
// /// 仅修正 <see cref="TouhouAncientEncounter"/> 挑战战斗的死亡文本。
// /// 原版用最后一个地图点的 <c>Rooms.First()</c> 记成事件死亡，先古之民没有
// /// <c>event.loss</c>，会落到 <c>MAP_POINT_HISTORY.debug</c>。
// /// 不改重拳出击、历战假人等原版事件（它们继续走 <c>event.loss</c>）。
// /// </summary>
// public static class TouhouAncientEncounterDeathQuotePatch
// {
//     /// <summary>
//     /// <see cref="NCombatRoom"/> 的私有字段 <c>_visuals</c>（<see cref="ICombatRoomVisuals"/>），
//     /// 用于读取当前战斗的 Encounter。
//     /// </summary>
//     private static readonly FieldInfo VisualsField =
//         AccessTools.Field(typeof(NCombatRoom), "_visuals");
//
//     /// <summary>
//     /// 获取当前正在进行的挑战战斗的 <see cref="TouhouAncientEncounter"/>。
//     /// <c>RunManager.Instance.State</c> 是私有属性无法直接访问，因此改用公开的
//     /// <see cref="NCombatRoom.Instance"/> 拿到当前战斗房间节点，再反射读取其
//     /// <c>_visuals.Encounter</c>（与 <see cref="TouhouAncientEncounterBgmNamePatch"/> 相同做法）。
//     /// 不在战斗房间或 Encounter 非挑战战斗时返回 null。
//     /// </summary>
//     private static TouhouAncientEncounter? GetCurrentTouhouAncientEncounter()
//     {
//         NCombatRoom? combatRoom = NCombatRoom.Instance;
//         if (combatRoom == null) return null;
//         ICombatRoomVisuals? visuals = VisualsField?.GetValue(combatRoom) as ICombatRoomVisuals;
//         return visuals?.Encounter as TouhouAncientEncounter;
//     }
//
//     [HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
//     public static class CreateRunHistoryEntry_Patch
//     {
//         static void Postfix(bool victory, bool isAbandoned)
//         {
//             if (victory || isAbandoned) return;
//             if (GetCurrentTouhouAncientEncounter() is not TouhouAncientEncounter encounter) return;
//
//             ModelId encounterId = encounter.Id;
//             RunHistory? history = RunManager.Instance.History;
//             if (history == null) return;
//             if (history.KilledByEncounter == encounterId && history.KilledByEvent == ModelId.none)
//             {
//                 return;
//             }
//
//             RunHistory corrected = new RunHistory
//             {
//                 SchemaVersion = history.SchemaVersion,
//                 PlatformType = history.PlatformType,
//                 GameMode = history.GameMode,
//                 Win = history.Win,
//                 Seed = history.Seed,
//                 StartTime = history.StartTime,
//                 RunTime = history.RunTime,
//                 Ascension = history.Ascension,
//                 BuildId = history.BuildId,
//                 WasAbandoned = history.WasAbandoned,
//                 KilledByEncounter = encounterId,
//                 KilledByEvent = ModelId.none,
//                 Players = history.Players,
//                 Acts = history.Acts,
//                 Modifiers = history.Modifiers,
//                 MapPointHistory = history.MapPointHistory
//             };
//             SaveManager.Instance.SaveRunHistory(corrected);
//             if (RunManager.Instance.IsInProgress)
//             {
//                 RunManager.Instance.History = corrected;
//             }
//         }
//     }
//
//     [HarmonyPatch(typeof(NRunHistory), nameof(NRunHistory.GetDeathQuote))]
//     public static class GetDeathQuote_Patch
//     {
//         static void Postfix(ModelId characterId, ref string __result)
//         {
//             if (GetCurrentTouhouAncientEncounter() is not TouhouAncientEncounter encounter) return;
//
//             CharacterModel character = SaveUtil.CharacterOrDeprecated(characterId);
//             LocString loss = encounter.GetLossMessageFor(character);
//             StringBuilder text = new StringBuilder();
//             text.Append(new LocString("game_over_screen", "ENCOUNTER_QUOTE_LEFT").GetRawText());
//             text.Append(loss.GetFormattedText());
//             text.Append(new LocString("game_over_screen", "ENCOUNTER_QUOTE_RIGHT").GetRawText());
//             __result = text.ToString();
//         }
//     }
// }
