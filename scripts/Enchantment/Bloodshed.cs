using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 喋血：每次打出恢复3生命。每场战斗这张牌第一次造成伤害时，额外恢复等同于造成伤害的生命值。
/// </summary>
public class Bloodshed : TouhouAncientEnchantmentModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HealVar(4m)
    ];

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    //public override bool HasExtraCardText => true;

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay == null) return;
        if (cardPlay?.Card != base.Card) return;

        await CreatureCmd.Heal(base.Card.Owner.Creature, base.DynamicVars.Heal.BaseValue);
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (cardSource != base.Card) return;
        if (base.Status != EnchantmentStatus.Normal) return;
        if (dealer == null) return;
        if (dealer != base.Card.Owner?.Creature) return;

        var healAmount = result.TotalDamage;
        if (healAmount <= 0) return;

        await CreatureCmd.Heal(dealer, healAmount);
        base.Status = EnchantmentStatus.Disabled;
    }
}
