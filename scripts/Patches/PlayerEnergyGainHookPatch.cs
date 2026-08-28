using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 仿 RitsuLib：Prefix 记下获得前能量，Postfix 把返回 Task 包一层，
/// 等 <see cref="PlayerCmd.GainEnergy"/> 真正加完能量后再分发
/// <see cref="IPlayerEnergyGainedListener.AfterPlayerEnergyGained"/>。
/// Postfix 优先级 -1，叠装 RitsuLib（Priority.Last == 0）时固定在其后触发。
/// </summary>
[HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy), [typeof(decimal), typeof(Player)])]
public static class PlayerEnergyGainHookPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(Player player, out int __state)
    {
        __state = player.PlayerCombatState?.Energy ?? 0;
    }

    [HarmonyPostfix]
    [HarmonyPriority(-1)]
    private static void Postfix(Player player, int __state, ref Task __result)
    {
        Task original = __result ?? Task.CompletedTask;
        __result = HarmonyAsyncTaskBridge.After(
            original,
            () => PlayerEnergyGainHook.AfterEnergyGainedIfChanged(player, __state));
    }
}
