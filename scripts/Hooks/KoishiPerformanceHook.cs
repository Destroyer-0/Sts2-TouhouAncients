using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.powers;
using TouhouAncients.Scripts.relics.IdleKoishi;

namespace TouhouAncients.Scripts.Hooks;

/// <summary>获得气氛后分发（<see cref="KoishiPerformanceCmd"/>）。</summary>
public interface IAtmosphereGainedListener
{
    Task AfterAtmosphereGained(PlayerChoiceContext choiceContext, int amount);
}

/// <summary>进入嗨到爆后分发。</summary>
public interface IHypedEnteredListener
{
    Task AfterHypedEntered(PlayerChoiceContext choiceContext);
}

/// <summary>离开嗨到爆前分发（此时仍可查到气氛与嗨到爆状态）。</summary>
public interface IHypedExitedListener
{
    Task AfterHypedExited(PlayerChoiceContext choiceContext);
}

/// <summary>气氛 / 嗨到爆的唯一入口：气氛只存在于此，遗物与卡牌通过上面的接口监听。</summary>
public static class KoishiPerformanceCmd
{
    public const int BaseThreshold = 14;

    public static int GetAtmosphere(Player player) =>
        player.Creature?.GetPowerAmount<AtmospherePower>() ?? 0;

    public static bool IsHyped(Player player) => player.Creature?.GetPower<HypedPower>() != null;

    /// <summary>进入嗨到爆所需气氛；特别门票每次额外回合会把它推高。</summary>
    public static int GetThreshold(Player player) =>
        BaseThreshold + (player.GetRelic<SpecialTicket>()?.ThresholdBonus ?? 0);

    public static async Task GainAtmosphere(PlayerChoiceContext choiceContext, Player player, int amount)
    {
        if (amount <= 0) return;

        var creature = player.Creature;
        if (creature == null || creature.IsDead || creature.CombatState == null) return;

        // 没有应援棒就不存在气氛（也避免控制台/其它途径拿到气氛来源后凭空计数）
        if (creature.GetPower<AtmospherePower>() == null && player.GetRelic<SupportBaton>() == null) return;

        await PowerCmd.Apply<AtmospherePower>(choiceContext, creature, amount, creature, null);

        await Dispatch(player, model => model is IAtmosphereGainedListener listener
            ? listener.AfterAtmosphereGained(choiceContext, amount)
            : Task.CompletedTask);

        await TryEnterHyped(choiceContext, player);
    }

    public static async Task TryEnterHyped(PlayerChoiceContext choiceContext, Player player)
    {
        if (IsHyped(player)) return;
        if (GetAtmosphere(player) < GetThreshold(player)) return;
        // 特别门票的额外回合禁止进入嗨到爆
        if (player.GetRelic<SpecialTicket>()?.IsHypedSuppressedThisTurn == true) return;

        await EnterHyped(choiceContext, player);
    }

    public static async Task EnterHyped(PlayerChoiceContext choiceContext, Player player)
    {
        var creature = player.Creature;
        if (creature == null || creature.CombatState == null || IsHyped(player)) return;

        // 先加状态再分发，监听者才能查到「已在嗨到爆」
        await PowerCmd.Apply<HypedPower>(choiceContext, creature, 1m, creature, null);

        await Dispatch(player, model => model is IHypedEnteredListener listener
            ? listener.AfterHypedEntered(choiceContext)
            : Task.CompletedTask);
    }

    public static async Task ExitHyped(PlayerChoiceContext choiceContext, Player player)
    {
        var creature = player.Creature;
        if (creature == null || !IsHyped(player)) return;

        await Dispatch(player, model => model is IHypedExitedListener listener
            ? listener.AfterHypedExited(choiceContext)
            : Task.CompletedTask);

        await PowerCmd.Remove<HypedPower>(creature);

        var atmosphere = creature.GetPower<AtmospherePower>();
        if (atmosphere is { Amount: > 0 })
        {
            // 归零即由 PowerCmd 自行移除
            await PowerCmd.ModifyAmount(choiceContext, atmosphere, -atmosphere.Amount, creature, null);
        }
    }

    /// <summary>只把事件分发给该玩家自己拥有的模型，避免联机下互相污染。</summary>
    private static async Task Dispatch(Player player, Func<AbstractModel, Task> action)
    {
        if (player.Creature?.CombatState is not { } combatState) return;

        foreach (var model in combatState.IterateHookListeners())
        {
            if (!OwnedBy(model, player)) continue;
            await action(model);
            model.InvokeExecutionFinished();
        }
    }

    private static bool OwnedBy(AbstractModel model, Player player) => model switch
    {
        RelicModel relic => relic.Owner == player,
        PowerModel power => power.Owner == player.Creature,
        CardModel card => card.Owner == player,
        PotionModel potion => potion.Owner == player,
        _ => false,
    };
}
