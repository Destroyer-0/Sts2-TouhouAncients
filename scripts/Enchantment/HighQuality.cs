using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.Enchantment;

public class HighQuality : CustomEnchantmentModel
{
    /// <summary>
    /// 上次计算出的费用减量，通过 [SavedProperty] 持久化存档。
    /// 读档时 Props.Fill 会在 OnEnchant 之前恢复此值，避免因 Owner 为 null 无法访问 Deck。
    /// </summary>
    [SavedProperty]
    private int TouhouAncients_EnergyDelta { get; set; }

    public override bool HasExtraCardText => false;

    public override bool CanEnchant(CardModel card)
    {
        return !card.HasStarCostX && !card.EnergyCost.CostsX && base.CanEnchant(card);
    }

    private void TryEnchantEnergyCost(int modify = 0)
    {
        if (!HasCard) return;

        if (Card.Owner == null)
        {
            // 反序列化阶段：Card.Owner 还未赋值，无法访问 Deck，
            // 从已恢复的保存属性中读取费用减量并应用。
            ApplyEnergyDelta(TouhouAncients_EnergyDelta);
            return;
        }

        GD.PrintErr($"附魔：优质{Card.Id.Entry}");
        var thisCard = Card;
        GD.PrintErr($"附魔2：优质{Card.Owner.Deck.Type}");
        var sameCard = Card.Owner.Deck.Cards.Count(x => x != thisCard && x.Id.Entry == thisCard.Id.Entry) + modify;
        GD.PrintErr($"一共有{sameCard}张同名卡牌{thisCard.Id.Entry}");

        TouhouAncients_EnergyDelta = sameCard;
        ApplyEnergyDelta(sameCard);
    }

    private void ApplyEnergyDelta(int delta)
    {
        // 直接用 SetCustomBaseCost 设置最终费用，不经过 UpgradeBy，
        // 避免 WasJustUpgraded 被设为 true 导致 UI 显示绿色费用文本。
        var newBase = Math.Max(0, Card.EnergyCost.Canonical - delta);
        Card.EnergyCost.SetCustomBaseCost(newBase);
    }

    protected override void OnEnchant()
    {
        TryEnchantEnergyCost();
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (!HasCard) return;
        CardPile? pile = card.Pile;

        // 同名卡加入牌组 → 重新计算减费
        if (pile != null && pile.Type == PileType.Deck && card.Owner == base.Card.Owner &&
            card.Id.Entry == base.Card.Id.Entry)
        {
            TryEnchantEnergyCost();
            return;
        }

        // 同名卡从牌组离开（移除/转换/消耗等）→ 重新计算减费
        if (pile == null && oldPileType == PileType.Deck && card.Owner == base.Card.Owner &&
            card.Id.Entry == base.Card.Id.Entry)
        {
            TryEnchantEnergyCost(-1);
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