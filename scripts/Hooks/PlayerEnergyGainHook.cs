using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Hooks;

/// <summary>
/// 玩家通过 <c>PlayerCmd.GainEnergy</c> 成功获得能量时的上下文。
/// </summary>
public readonly record struct PlayerEnergyGainContext(
    CombatState CombatState,
    Player Player,
    int Amount,
    int OldAmount,
    int NewAmount);

/// <summary>
/// 在 <c>PlayerCmd.GainEnergy</c> 真正加上能量之后运行。
/// 回合开始的 <c>ResetEnergy</c> / <c>AddMaxEnergyToCurrent</c> 不走这条路。
/// </summary>
public interface IPlayerEnergyGainedListener
{
    Task AfterPlayerEnergyGained(PlayerEnergyGainContext context);
}

/// <summary>
/// 将获得能量后的钩子分发给战斗中实现了 <see cref="IPlayerEnergyGainedListener"/> 的模型。
/// </summary>
public static class PlayerEnergyGainHook
{
    /// <summary>
    /// 将已构造好的获得能量上下文分发给战斗中的监听者。
    /// </summary>
    public static async Task AfterEnergyGained(PlayerEnergyGainContext context)
    {
        foreach (AbstractModel model in context.CombatState.IterateHookListeners())
        {
            if (model is IPlayerEnergyGainedListener listener)
            {
                await listener.AfterPlayerEnergyGained(context);
                model.InvokeExecutionFinished();
            }
        }
    }

    internal static async Task AfterEnergyGainedIfChanged(Player player, int oldAmount)
    {
        if (player.PlayerCombatState == null || player.Creature?.CombatState is not CombatState combatState)
        {
            return;
        }

        int newAmount = player.PlayerCombatState.Energy;
        int amount = newAmount - oldAmount;
        if (amount <= 0)
        {
            return;
        }

        await AfterEnergyGained(new PlayerEnergyGainContext(combatState, player, amount, oldAmount, newAmount));
    }
}

/// <summary>
/// Harmony 补丁替换异步返回值时，把续体接到原 Task 后面。
/// </summary>
internal static class HarmonyAsyncTaskBridge
{
    public static async Task After(Task originalTask, Func<Task> continuation)
    {
        ArgumentNullException.ThrowIfNull(originalTask);
        ArgumentNullException.ThrowIfNull(continuation);

        await originalTask;
        await continuation();
    }
}
