using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 喋血：首次造成伤害时，恢复等同于造成伤害的生命值。
/// </summary>
public class Bloodshed : CustomEnchantmentModel
{
    /// <summary>标记是否已经触发过回血效果（每个战斗实例重置）。</summary>
    private bool _hasTriggered;

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (_hasTriggered) return;
        if (cardSource != base.Card) return;
        if (dealer != base.Card.Owner?.Creature) return;

        var healAmount = result.TotalDamage;
        if (healAmount <= 0) return;

        _hasTriggered = true;
        await CreatureCmd.Heal(dealer, healAmount);
    }
}
