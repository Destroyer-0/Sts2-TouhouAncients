using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Saves;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 让 `unlock all` / `unlock monsters` 控制台指令也解锁本 Mod 的"先古之民"怪物，
/// 使它们在怪物图鉴（Bestiary）中显示为已发现。
///
/// 原版 <see cref="UnlockConsoleCmd.UnlockMonsters"/> 只遍历 <see cref="ModelDb.Monsters"/>
/// （= 各幕遭遇的怪物），本 Mod 挑战怪物（遭遇 IsValidForAct => false）不在其中，
/// 因此默认不会被解锁；此补丁在其执行后，把全部 <see cref="TouhouAncientMonsterBase"/>
/// 子类也按同样方式标记为已发现（FightStats.Wins = 1，对应图鉴的 TotalWins &gt; 0）。
/// </summary>
[HarmonyPatch(typeof(UnlockConsoleCmd), "UnlockMonsters")]
internal static class UnlockMonstersPatch
{
    [HarmonyPostfix]
    private static void AfterUnlockMonsters()
    {
        foreach (Type type in ModelDb.AllAbstractModelSubtypes)
        {
            if (type == null || type.IsAbstract || !type.IsSubclassOf(typeof(TouhouAncientMonsterBase)))
            {
                continue;
            }

            MonsterModel? monster = ModelDb.GetByIdOrNull<MonsterModel>(ModelDb.GetId(type));
            if (monster == null)
            {
                continue;
            }

            EnemyStats enemyStats = SaveManager.Instance.Progress.GetOrCreateEnemyStats(monster.Id);
            if (enemyStats.FightStats.Count == 0)
            {
                enemyStats.FightStats.Add(new FightStats
                {
                    Character = ModelDb.GetId<Ironclad>(),
                    Wins = 1
                });
            }
        }
    }
}
