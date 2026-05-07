using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 喋血：首次造成伤害时，恢复等同于造成伤害的生命值。
/// </summary>
public class Bloodshed : CustomEnchantmentModel
{
    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (cardSource != base.Card) return;
        if (dealer != base.Card.Owner?.Creature) return;

        var healAmount = result.TotalDamage;
        if (healAmount <= 0) return;

        await CreatureCmd.Heal(dealer, healAmount);
        base.Status = EnchantmentStatus.Disabled;
    }
}
