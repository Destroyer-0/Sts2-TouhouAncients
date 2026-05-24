using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 彷徨：如果打出的上一张牌是攻击牌，则打出后回到你的手中并获得10格挡。
/// </summary>
public class Wandering : CustomEnchantmentModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BlockAmount", 8m),
        new DynamicVar("RemainingChance", 3m),
        new DynamicVar("BaseRemainingChance", 3m),
    ];

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Skill;
    }

    public override bool HasExtraCardText => true;

    public override bool ShouldGlowGold =>
        CombatManager.Instance.History.CardPlaysFinished.LastOrDefault(x =>
                x.HappenedThisTurn(Card.Owner.Creature.CombatState)) is
            { CardPlay.Card.Type: CardType.Attack } or && base.DynamicVars["RemainingChance"].IntValue > 0;

    // public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    // {
    //     if (!HasCard) return;
    //     if (base.Status != EnchantmentStatus.Normal) return;
    //
    //     var player = base.Card.Owner;
    //     var playerId = player.NetId;
    //
    //     // 如果打出的是本附魔所在的卡牌
    //     if (cardPlay.Card == base.Card)
    //     {
    //         if (CombatManager.Instance.History.CardPlaysFinished.LastOrDefault() is
    //             { CardPlay.Card.Type: CardType.Attack })
    //         {
    //             // 回到手中并获得格挡
    //             shouldBack = remainingChance > 0;
    //         }
    //     }
    // }


    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (!HasCard) return;
        if (card != base.Card)
        {
            return;
        }

        var shouldBack = DynamicVars["RemainingChance"].IntValue > 0 &&
                         CombatManager.Instance.History.CardPlaysFinished
                                 .Where(x => x.HappenedThisTurn(Card.Owner.Creature.CombatState))
                                 .SkipLast(1)
                                 .LastOrDefault() is
                             { CardPlay.Card.Type: CardType.Attack };

        if (shouldBack && oldPileType == PileType.Play && card.Pile != null && card.Pile.Type != PileType.None)
        {
            var blockAmount = base.DynamicVars["BlockAmount"].BaseValue;
            await CreatureCmd.GainBlock(card.Owner.Creature, blockAmount, ValueProp.Unpowered, null);
            await CardPileCmd.Add(base.Card, PileType.Hand, source: this);
            DynamicVars["RemainingChance"].UpgradeValueBy(-1);
            if (DynamicVars["RemainingChance"].IntValue <= 0)
            {
                base.Status = EnchantmentStatus.Disabled;
            }
        }
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Card.Owner) return Task.CompletedTask;
        base.Status = EnchantmentStatus.Normal;
        return Task.CompletedTask;
    }
}