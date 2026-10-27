using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.Hooks;
using TouhouAncients.Scripts.relics.IdleKoishi;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 蔷薇：嗨到爆状态下耗能 -1 并获得保留；打出后对全体敌人造成 5 点伤害，
/// 并使蔷薇的伤害效果本回合 +5（累计在偶像专辑上，回合末清空）。
/// </summary>
public class Rose : TouhouAncientEnchantmentModel
{
    private const int EnergyReduction = 1;
    private const decimal BaseDamage = 5m;
    private const decimal DamageIncrease = 5m;

    public override bool HasExtraCardText => false;

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

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card) return;

        var player = Card.Owner;
        if (player?.Creature?.CombatState is not { } combatState) return;

        var album = player.GetRelic<IdolAlbum>();
        var damage = base.DynamicVars["Damage"].BaseValue + (album?.RoseBonusThisTurn ?? 0);

        await DamageCmd.Attack(damage).FromCard(Card, cardPlay).TargetingAllOpponents(combatState)
            .Execute(choiceContext);

        album?.AddRoseBonus((int)DamageIncrease);
    }
}
