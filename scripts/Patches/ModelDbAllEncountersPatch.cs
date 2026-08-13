using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.encounters;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 让全局遭遇枚举 <see cref="ModelDb.AllEncounters"/> 也包含本 Mod 的"先古之民"挑战遭遇，
/// 使依赖该枚举的第三方工具能反查到这些遭遇——例如意图状态机 mod（intentgraph2）在怪物
/// 图鉴里生成意图图时，就是用 <c>ModelDb.AllEncounters.Where(e => e.AllPossibleMonsters
/// .Any(m => m.Id == monster.Id))</c> 来定位包含所选怪物的遭遇，进而生成状态机图。
///
/// 原版 <see cref="ModelDb.AllEncounters"/> 由各幕（Act）遭遇与官方事件遭遇组成
/// （= Acts.SelectMany(a => a.AllEncounters).Concat(EventEncounters).Distinct()），
/// 本 Mod 挑战遭遇（IsValidForAct => false）既不在任何幕中，也不属于 EventEncounters，
/// 因此不会出现在该枚举里，导致意图图无法生成；此补丁在其结果末尾追加全部挑战遭遇。
///
/// 注意：普通/精英/Boss 房间的遭遇生成走的是各幕自己的遭遇池
/// （ActModel.AllEncounters → 各幕 GenerateAllEncounters()），不使用 ModelDb.AllEncounters，
/// 因此追加挑战遭遇不会让它们在普通跑图中出现；仅影响全解锁状态（UnlockState.all）、
/// fight 控制台命令候选列表，以及依赖此枚举的第三方工具。
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllEncounters), MethodType.Getter)]
internal static class ModelDbAllEncountersPatch
{
    /// <summary>本 Mod 的挑战遭遇（均在"先古之民"Ancient 分区中展示）。</summary>
    private static readonly EncounterModel[] ChallengeEncounters =
    [
        ModelDb.Encounter<HakureiReimuEncounter>(),
        ModelDb.Encounter<KirisameMarisaEncounter>(),
        ModelDb.Encounter<YorigamiSistersEncounter>()
    ];

    [HarmonyPostfix]
    private static IEnumerable<EncounterModel> AppendChallengeEncounters(IEnumerable<EncounterModel> __result)
    {
        return __result.Concat(ChallengeEncounters).Distinct();
    }
}
