using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 铃兰：0费。技能。保留。升级后获得固有。
/// 获得1能量，抽2张牌，给予自身0层中毒。
/// 回合结束时，如果这张牌在你的手中，给予所有敌人2（升级后3）层中毒，获得的能量增加1，给予自身的中毒层数+1。
/// </summary>
[Pool(typeof(EventCardPool))]
public class LilyBell : TouhouAncientCards
{
    private const int energyCost = 0;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(2),
        new DynamicVar("Multiplier", 3),
        new DynamicVar("ExtraAmount", 2),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatePoison").WithMultiplier((CardModel card, Creature? my) =>
            card.Affliction is not Tainted tainted
                ? 0
                : (tainted.Amount + card.Owner.Creature.GetPowerAmount<TaintedPower>()) * (card.IsUpgraded ? 4 : 3))
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromAffliction<Tainted>()
        .Append(HoverTipFactory.FromPower<PoisonPower>());

    public LilyBell() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card != this) return;
        await TryAfflictTainted(0);
    }

    private async Task TryAfflictTainted(int amount)
    {
        if (this.Affliction is Tainted tainted)
        {
            tainted.Amount += amount;
        }
        else
        {
            await CardCmd.Afflict<Tainted>(this, amount);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != this) return;
        var player = base.Owner;

        // 获得能量
        var totalEnergy = base.DynamicVars.Energy.BaseValue;
        if (totalEnergy > 0)
        {
            await PlayerCmd.GainEnergy(totalEnergy, player);
        }

        // 抽2张牌
        await CardPileCmd.Draw(choiceContext, base.DynamicVars["Cards"].IntValue, player, fromHandDraw: true);
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        if (base.CombatState == null) return;
        await TryAfflictTainted(DynamicVars["ExtraAmount"].IntValue);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != this) return;
        if (base.CombatState == null) return;
        if (CombatState.Creatures.All(x => !x.HasPower<VitalSparkPower>()))
        {
            if (cardPlay.Card.Affliction is Tainted tainted)
            {
                await PowerCmd.Apply<TaintedPower>(choiceContext, cardPlay.Card.Owner.Creature, tainted.Amount, null,
                    null);
            }
        }
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != this) return;
        if (base.CombatState == null) return;
        await TryAfflictTainted(DynamicVars["ExtraAmount"].IntValue);
        var enemies = base.CombatState.HittableEnemies.ToList();
        var totalPoison = Owner.Creature.GetPowerAmount<TaintedPower>() * base.DynamicVars["Multiplier"].IntValue;
        if (totalPoison > 0)
        {
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemies, totalPoison, Owner.Creature, this);
        }
    }

    /// <summary>
    /// 升级：对敌中毒+1，获得固有（通过CanonicalKeywords处理）。
    /// </summary>
    protected override void OnUpgrade()
    {
        base.DynamicVars["Multiplier"].UpgradeValueBy(1m);
    }
}

/// <summary>
/// Patch VitalSparkPower 的 AfterCardPlayed，使其在打出带有 Tainted（苦恼症）的牌时，
/// 基于 Tainted 的层数（而非 VitalSparkPower 自身的层数）施加 TaintedPower。
/// </summary>
[HarmonyPatch]
public static class VitalSparkPatch
{
    private static MethodBase TargetMethod()
    {
        return typeof(VitalSparkPower).GetMethod("AfterCardPlayed");
    }

    [HarmonyPrefix]
    private static bool Postfix(VitalSparkPower __instance, ref Task __result, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        try
        {

            if (cardPlay.Card.Affliction is not Tainted a)
            {
                __result = Task.CompletedTask;
                return true;
            }

            var originalTask = __result;
            GD.PrintErr($"铃兰： {a.Amount}");
            __result = ContinueAsync(__instance, choiceContext, cardPlay.Card.Owner.Creature, a.Amount);
            return false;
        }
        catch (System.Exception e)
        {
            Log.Error(e.ToString());
        }
        return true;
    }

    private static async Task ContinueAsync(AbstractModel instance,
        PlayerChoiceContext choiceContext, Creature target, int amount)
    {
        AccessTools.Method(typeof(PowerModel), "Flash").Invoke(instance, null);
        await PowerCmd.Apply<TaintedPower>(choiceContext, target, amount, null, null);
    }
}