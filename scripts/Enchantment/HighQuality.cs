using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

public class HighQuality : CustomEnchantmentModel
{
    public override bool HasExtraCardText => false;

    public override bool CanEnchant(CardModel card)
    {
        return !card.HasStarCostX && !card.EnergyCost.CostsX && base.CanEnchant(card);
    }

    private void TryEnchantEnergyCost(int modify = 0)
    {
        if (!HasCard) return;
        GD.PrintErr($"附魔：优质{Card.Id.Entry}");
        var thisCard = Card;
        GD.PrintErr($"附魔2：优质{Card.Owner.Deck.Type}");
        var sameCard = Card.Owner.Deck.Cards.Count(x => x.Id.Entry == thisCard.Id.Entry) - 1 + modify;
        GD.PrintErr($"一共有{sameCard}张同名卡牌{thisCard.Id.Entry}");
        base.Card.EnergyCost.ResetForDowngrade();
        base.Card.EnergyCost.UpgradeBy(-1 * sameCard);
    }

    protected override void OnEnchant()
    {
        TryEnchantEnergyCost();
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (!HasCard) return;
        CardPile? pile = card.Pile;
        GD.PrintErr($"带有优质附魔的卡牌{card.Id.Entry}/{oldPileType}/{pile == null}");
        if (pile != null)
        {
            GD.PrintErr(
                $"卡牌所在牌堆类型{pile.Type}，卡牌所属玩家{card.Owner == base.Card.Owner}，卡牌ID是否相同{card.Id.Entry == base.Card.Id.Entry}");
        }

        if (pile != null && pile.Type == PileType.Deck && card.Owner == base.Card.Owner &&
            card.Id.Entry == base.Card.Id.Entry)
        {
            TryEnchantEnergyCost();
            return;
        }
    }

    public override async Task BeforeCardRemoved(CardModel card)
    {
        if (!HasCard) return;
        GD.PrintErr($"带有优质附魔的卡牌{card.Id.Entry}被移除。");
        if (card.Owner == base.Card.Owner &&
            card.Id.Entry == base.Card.Id.Entry)
        {
            TryEnchantEnergyCost(-1);
        }
    }
}