using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts;
using TouhouAncients.Scripts.encounters;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 遭遇战失败文本的角色彩蛋，以及挑战战斗死亡被记成事件死亡时的纠正。
///
/// 1. Postfix <see cref="EncounterModel.GetLossMessageFor"/>：优先用
///    {EncounterId}.loss.{token}（token 是 Character.Id.Entry 的子串），否则保持原版 .loss。
///    GetLossMessageFor 不是虚方法，只能打补丁。
/// 2. Postfix <see cref="RunHistoryUtilities.CreateRunHistoryEntry"/>：挑战从事件里
///    EnterCombatWithoutExitingEvent 进入，地图点 Rooms.First() 仍是事件，原版会写成
///    KilledByEvent，先古没有 event.loss。若 Rooms 里最后一场战斗是
///    <see cref="TouhouAncientEncounter"/>，改记为 KilledByEncounter，死亡引言才会走
///    GetLossMessageFor（含 loss.mokou 等变体）。
/// </summary>
public static class EncounterLossMessagePatch
{
    [HarmonyPatch(typeof(EncounterModel), nameof(EncounterModel.GetLossMessageFor))]
    public static class GetLossMessageFor_Patch
    {
        [HarmonyPostfix]
        static void Postfix(EncounterModel __instance, CharacterModel character, ref LocString __result)
        {
            LocString? variant = CharacterLocVariant.Find(
                "encounters",
                $"{__instance.Id.Entry}.loss.",
                "",
                character.Id.Entry);
            if (variant == null) return;

            character.AddDetailsTo(variant);
            variant.Add("encounter", __instance.Title);
            __result = variant;
        }
    }

    [HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
    public static class CreateRunHistoryEntry_Patch
    {
        [HarmonyPostfix]
        static void Postfix(SerializableRun run, bool victory, bool isAbandoned)
        {
            if (victory || isAbandoned) return;

            ModelId encounterId = FindTouhouAncientChallengeEncounterId(run);
            if (encounterId == ModelId.none) return;

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

        /// <summary>
        /// 当前地图点若叠了挑战战斗，Rooms.Last() 的战斗房间才是真正的死因。
        /// </summary>
        private static ModelId FindTouhouAncientChallengeEncounterId(SerializableRun run)
        {
            MapPointRoomHistoryEntry? combatRoom = run.MapPointHistory
                .LastOrDefault()
                ?.LastOrDefault()
                ?.Rooms.LastOrDefault(r => r.RoomType.IsCombatRoom());
            if (combatRoom == null) return ModelId.none;

            EncounterModel encounter = SaveUtil.EncounterOrDeprecated(combatRoom.ModelId);
            return encounter is TouhouAncientEncounter ? encounter.Id : ModelId.none;
        }
    }
}
