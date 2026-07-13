using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 冈格尼尔：拾起时，将一张神枪「冈格尼尔」加入牌组。
/// 你获得格挡时，所有敌人获得等量格挡。
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

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != base.Owner.Creature) return;
        if (amount <= 0m) return;

        Flash();

        var enemies = base.Owner.Creature.CombatState.HittableEnemies;
        foreach (var enemy in enemies)
        {
            await CreatureCmd.GainBlock(enemy, amount, ValueProp.Unpowered, null, fast: true);
        }
    }
}
