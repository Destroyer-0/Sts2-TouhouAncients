using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 蔷薇：嗢到爆状态下耗能 -1 并获得保留；打出后对全体敌人造成 5 点伤害，
/// 本次战斗中每打出过一次（重放算多次），伤害再 +5。
/// </summary>
public class Rose : TouhouAncientEnchantmentModel
{
    private const int EnergyReduction = 1;
    private const decimal BaseDamage = 5m;
    private const decimal DamageIncrease = 5m;

    public override bool HasExtraCardText => true;

    /// <summary>专属附魔，不进入随机附魔池。</summary>
    public override bool CanBeRandomSelected => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar("EnergyReduction", 1),
        new DamageVar("Damage", BaseDamage, ValueProp.Move),
        new DynamicVar("DamageIncrease", DamageIncrease),
    ];

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != Card || !HasCard) return false;
        if (card.EnergyCost.CostsX) return false;
        if (card.Owner == null || !KoishiPerformanceCmd.IsHyped(card.Owner)) return false;

        modifiedCost = Math.Max(0m, originalCost - EnergyReduction);
        return true;
    }

    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (card != Card || !HasCard) return false;
        if (card.Owner == null || !KoishiPerformanceCmd.IsHyped(card.Owner)) return false;

        keywords.Add(CardKeyword.Retain);
        return true;
    }

    /// <summary>已打出的次数（含重放的每一次）；本次打出的记录在本方法调用后才会写入历史。</summary>
    private int PreviousPlays(CardModel card)
    {
        if (card.Owner is not { } owner) return 0;
        return CombatManager.Instance.History.CardPlaysFinished.Count(entry =>
            entry.CardPlay.Card.Owner == owner && entry.CardPlay.Card.Enchantment is Rose);
    }

    /// <summary>卡面叠得实时刷新：CombatStateChanged 时原版会遍历所有牌回调这里。</summary>
    public override void RecalculateValues()
    {
        if (!HasCard) return;
        base.DynamicVars["Damage"].BaseValue = BaseDamage + PreviousPlays(Card) * DamageIncrease;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card) return;

        var player = Card.Owner;
        if (player?.Creature?.CombatState is not { } combatState) return;

        // 连打/重放时不能靠缓存值，打出前重算一次
        RecalculateValues();
        decimal damage = base.DynamicVars["Damage"].BaseValue;

        await DamageCmd.Attack(damage).FromCard(Card, cardPlay).TargetingAllOpponents(combatState)
            .Execute(choiceContext);
    }
}
