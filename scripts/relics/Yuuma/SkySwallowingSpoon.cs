using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 吞天之勺：向牌组中加入卡牌时，吞噬之（不加入牌组）并获得4最大生命。
/// 通过 Harmony Patch 在卡牌加入牌组后立即移除。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SkySwallowingSpoon : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";

    [SavedProperty]
    public int TouhouAncients_SwallowedCards
    {
        get => swallowedCards;
        set
        {
            AssertMutable();
            swallowedCards = value;
            InvokeDisplayAmountChanged();
        }
    }

    private int swallowedCards;

    public override bool HasUponPickupEffect => false;
    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_SwallowedCards;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(5)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
    ];

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (base.Owner.Creature.IsDead || card.Owner != base.Owner)
        {
            return;
        }
        CardPile? pile = card.Pile;
        if (pile != null && pile.Type == PileType.Deck && card.Enchantment == null)
        {
            Flash();
            CardCmd.Enchant<BloodPond>(card, 1m);
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        var list = Owner.Deck.Cards
            .Where(c => c.Enchantment is BloodPond)
            .ToList();
        if (list.Count > 0)
        {
            Flash();
            foreach (var cardModel in list)
            {
                TouhouAncients_SwallowedCards++;
                await CardPileCmd.RemoveFromDeck(cardModel);
                await CreatureCmd.GainMaxHp(Owner.Creature,
                    cardModel.Type == CardType.Curse ? 2 : 1 * base.DynamicVars["MaxHp"].IntValue);
            }
            NCombatRoom.Instance?.GetCreatureNode(base.Owner.Creature)
                ?.ScaleTo(MathF.Min(2.5f, 1 + TouhouAncients_SwallowedCards * 0.05f), 0f);
        }
        return;
    }
}