// using System.Linq;
// using HarmonyLib;
// using MegaCrit.Sts2.Core.Models;
// using MegaCrit.Sts2.Core.Rooms;
// using MegaCrit.Sts2.Core.Runs;
// using MegaCrit.Sts2.Core.Runs.History;
// using MegaCrit.Sts2.Core.Saves;
//
// namespace TouhouAncients.Scripts.Patches;
//
// /// <summary>
// /// 修复"从先古之民挑战选项进入的战斗"死亡后 Encounter 死亡文本无法生效的问题。
// ///
// /// 背景：先古之民的挑战战斗参照 PUNCH_OFF（重拳出击）等可选战斗事件，通过
// /// EnterCombatWithoutExitingEvent 从事件房间进入战斗；原版
// /// <see cref="RunHistoryUtilities.CreateRunHistoryEntry"/> 用 MapPointHistory 最后一条记录的
// /// Rooms.First()（第一个房间）判定死亡来源，而 EnterRoomWithoutExitingCurrentRoom 会把战斗
// /// 房间追加到 Rooms 末尾，于是第一个房间是父事件房间（Event 类型）——killedByEncounter 未被
// /// 设置、killedByEvent 被设为先古之民事件。GameOverType 因此判定为 EventDeath，去查找事件的
// /// .loss 键（先古之民没有）后回退显示 MAP_POINT_HISTORY.debug，Encounter 的 .loss 文本不会
// /// 生效。
// ///
// /// 本补丁仿照上述可选战斗事件的处理思路，patch 拥有战斗的先古之民房间：在
// /// CreateRunHistoryEntry 之后（Postfix）检测"最后一条记录以战斗收尾（最后一个房间是战斗）但
// /// 第一个房间不是（即从事件进入的战斗）"且该战斗 Encounter 是 mod 自己的
// /// （TOUHOUANCIENTS- 前缀）的场景，把 killedByEncounter 修正为该战斗的 Encounter、
// /// killedByEvent 置空，并重新保存 RunHistory、更新 RunManager.History。这样死亡 UI 走
// /// CombatDeath 分支，正常激活并显示 Encounter 的 .loss 文本（如
// /// TOUHOUANCIENTS-KIRISAME_MARISA_ENCOUNTER.loss）。
// ///
// /// 仅对 mod 自己的遭遇战生效，不影响原版事件战斗（PUNCH_OFF / DenseVegetation 等）的既有
// /// 死亡文本行为。
// /// </summary>
// [HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
// public static class RunHistoryAncientCombatPatch
// {
//     static void Postfix(SerializableRun run, bool victory, bool isAbandoned)
//     {
//         // 只有非胜利、非主动放弃的死亡场景才需要修正死亡来源。
//         if (victory || isAbandoned)
//         {
//             return;
//         }
//
//         MapPointHistoryEntry? lastEntry = run.MapPointHistory.LastOrDefault()?.LastOrDefault();
//         if (lastEntry == null || lastEntry.Rooms.Count == 0)
//         {
//             return;
//         }
//
//         // 仅修正"最后一个房间是战斗、但第一个房间不是"（从事件进入的战斗）的场景。
//         // 第一个房间就是战斗时，原版已正确记录 killedByEncounter，无需处理。
//         MapPointRoomHistoryEntry lastRoom = lastEntry.Rooms[lastEntry.Rooms.Count - 1];
//         if (!lastRoom.RoomType.IsCombatRoom())
//         {
//             return;
//         }
//         if (lastEntry.Rooms[0].RoomType.IsCombatRoom())
//         {
//             return;
//         }
//
//         // 只处理 mod 自己的遭遇战，避免改变原版事件战斗的死亡文本行为。
//         ModelId? encounterId = lastRoom.ModelId;
//         if (encounterId == null || !encounterId.Entry.StartsWith("TOUHOUANCIENTS-"))
//         {
//             return;
//         }
//
//         RunHistory? history = RunManager.Instance.History;
//         if (history == null || history.KilledByEncounter == encounterId)
//         {
//             return;
//         }
//
//         // RunHistory 的属性均为 init-only，只能重新构造实例后覆盖保存（StartTime 相同则
//         // 覆盖同一个 .run 文件），并更新运行时 History 供游戏结束界面读取。
//         RunHistory corrected = new RunHistory
//         {
//             SchemaVersion = history.SchemaVersion,
//             PlatformType = history.PlatformType,
//             GameMode = history.GameMode,
//             Win = history.Win,
//             Seed = history.Seed,
//             StartTime = history.StartTime,
//             RunTime = history.RunTime,
//             Ascension = history.Ascension,
//             BuildId = history.BuildId,
//             WasAbandoned = history.WasAbandoned,
//             KilledByEncounter = encounterId,
//             KilledByEvent = ModelId.none,
//             Players = history.Players,
//             Acts = history.Acts,
//             Modifiers = history.Modifiers,
//             MapPointHistory = history.MapPointHistory
//         };
//         SaveManager.Instance.SaveRunHistory(corrected);
//         RunManager.Instance.History = corrected;
//     }
// }
