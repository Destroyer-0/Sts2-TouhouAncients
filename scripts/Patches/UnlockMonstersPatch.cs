using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 让全局怪物枚举 <see cref="ModelDb.Monsters"/> 也包含本 Mod 的"先古之民"挑战怪物，
/// 使 `unlock all` / `unlock monsters` 控制台指令能解锁它们（原版指令遍历
/// <see cref="ModelDb.Monsters"/> 后统一按 FightStats.Wins = 1 标记为已发现，
/// 对应图鉴的 TotalWins &gt; 0）。
///
/// 原版 <see cref="ModelDb.Monsters"/> 由各幕遭遇的怪物与官方事件遭遇的怪物组成
/// （= Acts.SelectMany(a => a.AllMonsters).Concat(EventEncounters.SelectMany(e =>
/// e.AllPossibleMonsters)).Distinct()），本 Mod 挑战怪物（遭遇 IsValidForAct => false，
/// 且不属于 EventEncounters）不在其中，因此默认不会被解锁；此补丁在其结果末尾追加全部
/// <see cref="TouhouAncientMonsterBase"/> 子类。
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Monsters), MethodType.Getter)]
internal static class UnlockMonstersPatch
{
    [HarmonyPostfix]
    private static IEnumerable<MonsterModel> AppendChallengeMonsters(IEnumerable<MonsterModel> __result)
    {
        return __result.Concat(GetChallengeMonsters()).Distinct();
    }

    /// <summary>枚举全部 <see cref="TouhouAncientMonsterBase"/> 子类的 canonical 实例。</summary>
    private static IEnumerable<MonsterModel> GetChallengeMonsters()
    {
        foreach (Type type in ModelDb.AllAbstractModelSubtypes)
        {
            if (type == null || type.IsAbstract || !type.IsSubclassOf(typeof(TouhouAncientMonsterBase)))
            {
                continue;
            }

            MonsterModel? monster = ModelDb.GetByIdOrNull<MonsterModel>(ModelDb.GetId(type));
            if (monster != null)
            {
                yield return monster;
            }
        }
    }
}
