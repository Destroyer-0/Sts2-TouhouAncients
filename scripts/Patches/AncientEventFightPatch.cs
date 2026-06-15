using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rewards;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 在所有 AncientEventModel 的选项末尾追加"战斗"选项。
/// 玩家选择战斗后，进入 PunchOffEventEncounter 战斗，
/// 胜利后获得 200 金币 + 当前页面展示的所有遗物（代替只能选一个）。
/// </summary>
[HarmonyPatch]
public static class AncientEventFightPatch
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AncientEventModel), "GenerateInitialOptionsWrapper")]
    private static void AddFightOption(AncientEventModel __instance, ref IReadOnlyList<EventOption> __result)
    {
        // 排除涅奥——新手流程不能战斗
        if (__instance is Neow) return;

        var options = __result.ToList();

        // 从当前选项取出所有遗物实例，供战后奖励使用
        var allRelicModels = options
            .Select(o => o.Relic)
            .Where(r => r != null)
            .OfType<RelicModel>()
            .ToList();
        
        // 追加"战斗"选项
        options.Add(new EventOption(
            __instance,
            () => Fight(__instance, allRelicModels),
            "TOUHOUANCIENTS_ANCIENT_FIGHT"
        ));

        __result = options;
    }

    private static async Task Fight(AncientEventModel ancient,IReadOnlyList<RelicModel> relicIds)
    {
        var player = ancient.Owner;

        // 取出之前保存的遗物 ID 列表

        // 构建奖励：200 金币 + 之前展示的所有遗物
        var rewards = new List<Reward>
        {
            new GoldReward(200, player)
        };

        foreach (var relicId in relicIds)
        {
            relicId.Owner = player;
            rewards.Add(new RelicReward(relicId, player));
        }

        // TODO: 后续可替换为每个 Ancient 专属战斗 Encounter
        var encounter = ModelDb.Encounter<PunchOffEventEncounter>().ToMutable();

        // 通过反射调用 protected 方法 EnterCombatWithoutExitingEvent
        var method = AccessTools.DeclaredMethod(typeof(EventModel), "EnterCombatWithoutExitingEvent",
            new[] { typeof(EncounterModel), typeof(IReadOnlyList<Reward>), typeof(bool) });
        method.Invoke(ancient, new object[] { encounter, rewards, false });
    }
}
