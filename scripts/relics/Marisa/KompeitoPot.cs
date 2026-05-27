using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 金平糖罐：你的卡牌奖励中将额外出现一张升级过的能力牌，选取3张后失效。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class KompeitoPot : TouhouAncientRelics
{
    private const int MaxUses = 3;

    [SavedProperty] public int TouhouAncients_UsesRemaining { get; set; } = MaxUses;

    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_UsesRemaining;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("UsesRemaining", MaxUses)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        Enumerable.Empty<IHoverTip>();

    public override bool HasUponPickupEffect => false;

    private CardModel? spawnedCard;

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> options,
        CardCreationOptions creationOptions)
    {
        if (base.Owner != player) return false;
        if (TouhouAncients_UsesRemaining <= 0) return false;
        if (creationOptions.Source != CardCreationSource.Encounter) return false;

        // 找一张能力牌
        IEnumerable<CardModel> enumerable = from c in creationOptions.GetPossibleCards(player)
            where c.Type == CardType.Power && options.TrueForAll((CardCreationResult o) => o.originalCard.Id != c.Id)
            select c;
        if (!enumerable.Any())
        {
            enumerable = from c in creationOptions.GetPossibleCards(player)
                where c.Type == CardType.Power
                select c;
        }

        if (!enumerable.Any())
        {
            return false;
        }

        CardCreationOptions options2 =
            new CardCreationOptions(enumerable, CardCreationSource.Other, creationOptions.RarityOdds).WithFlags(
                CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications);
        CardModel cardModel = CardFactory.CreateForReward(base.Owner, 1, options2).FirstOrDefault()?.Card;
        if (cardModel != null)
        {
            if (cardModel.IsUpgradable)
            {
                CardCmd.Upgrade(cardModel);
            }

            CardCreationResult cardCreationResult = new CardCreationResult(cardModel);
            cardCreationResult.ModifyCard(cardModel, this);
            spawnedCard = cardModel;
            options.Add(cardCreationResult);
        }

        return cardModel != null;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Owner != base.Owner) return;
        if (spawnedCard == null) return;
        CardPile? pile = card.Pile;
        if (pile != null && pile.Type == PileType.Deck && card == spawnedCard)
        {
            Flash();
            TouhouAncients_UsesRemaining--;
            InvokeDisplayAmountChanged();

            if (TouhouAncients_UsesRemaining <= 0)
            {
                base.Status = MegaCrit.Sts2.Core.Entities.Relics.RelicStatus.Disabled;
            }
        }
    }
}