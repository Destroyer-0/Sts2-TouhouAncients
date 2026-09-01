using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 冈格尼尔：拾起时，将一张神枪「冈格尼尔」加入牌组。
/// 你使用神枪「冈格尼尔」以外的卡牌造成伤害或获得格挡时，为目标附加等量格挡。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SpearGungnir : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<GungnirSpearCard>();

    public override async Task AfterObtained()
    {
        CardModel card = base.Owner.RunState.CreateCard<GungnirSpearCard>(base.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (!TouhouAncientCmd.IsPlayerDamageIncludePet(Owner, dealer)) return;
        if (cardSource == null) return;
        if (!props.IsPoweredAttack()) return;
        if (result.UnblockedDamage <= 0) return;
        if (cardSource is GungnirSpearCard) return;
        if (result.TotalDamage <= 0m) return;

        Flash();

        await CreatureCmd.GainBlock(target, result.UnblockedDamage, ValueProp.Unpowered, null, fast: true);
    }

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props,
        CardModel? cardSource)
    {
        if (creature != base.Owner.Creature) return;
        if (cardSource == null) return;
        if (cardSource is GungnirSpearCard) return;
        if (amount <= 0m) return;

        Flash();

        var enemies = base.Owner.Creature.CombatState.HittableEnemies;
        foreach (var enemy in enemies)
        {
            await CreatureCmd.GainBlock(enemy, amount, ValueProp.Unpowered, null, fast: true);
        }
    }
}