using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 辉夜姬秘宝：
/// 每场战斗开始时，从7张其它角色的牌库中选择一张加入手牌。
/// 当你打出这张牌时，其原始复制会加入本次的掉落奖励。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class KaguyaSecretTreasure : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(12)
    ];

    public HashSet<CardModel> _selectedCardModel = new();

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner || base.Owner.Creature.CombatState.RoundNumber != 1)
        {
            return;
        }

        _selectedCardModel.Clear();
        Flash();
        var myPool = player.Character.CardPool;
        var otherPools = player.UnlockState.CharacterCardPools
            .Where(p => !ReferenceEquals(p, myPool))
            .ToList();

        if (otherPools.Count == 0) return;

        // 从其他角色池中收集所有可用的牌，随机取 OfferCount 张

        List<CardPoolModel> list = base.Owner.UnlockState.CharacterCardPools.ToList();
        if (list.Count > 1)
        {
            list.Remove(base.Owner.Character.CardPool);
        }

        IEnumerable<CardModel> cards = from c in list.SelectMany((CardPoolModel c) =>
                c.GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint))
            select c;
        List<CardModel> list2 = CardFactory.GetDistinctForCombat(base.Owner, cards, DynamicVars.Cards.IntValue,
            base.Owner.RunState.Rng.CombatCardGeneration).ToList();

        if (list2.Count == 0) return;

        // 让玩家从网格中选择一张
        foreach (var cardModel in await CardSelectCmd.FromSimpleGrid(
                     context: choiceContext,
                     cardsIn: list2,
                     player: player,
                     prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1)
                 ))
        {
            await CardPileCmd.AddGeneratedCardToCombat(cardModel, PileType.Hand, addedByPlayer: true);
            _selectedCardModel.Add(cardModel);
        }
    }


    /// <summary>
    /// 检测选中的牌是否被玩家打出。
    /// </summary>
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (base.Owner.Creature.CombatState == null) return Task.CompletedTask;
        if (!_selectedCardModel.Contains(cardPlay.Card)) return Task.CompletedTask;
        Flash();
        Status = RelicStatus.Active;

        AbstractRoom currentRoom = base.Owner.Creature.CombatState!.RunState.CurrentRoom;
        if (currentRoom is CombatRoom combatRoom)
        { 
            var card = ModelDb.GetById<CardModel>(cardPlay.Card.Id).ToMutable();
            if (cardPlay.Card.IsUpgraded)
            {
                CardCmd.Upgrade(card);
            }
            IRunState runState = base.Owner.RunState;
            runState.AddCard(card, base.Owner);
            SpecialCardReward specialCardReward = new SpecialCardReward(card, base.Owner);
            combatRoom.AddExtraReward(base.Owner, specialCardReward);
            // var newCard = ModelDb.GetById<CardModel>(cardPlay.Card.Id);
            // var options = new CardCreationOptions([newCard], CardCreationSource.Other, CardRarityOddsType.Uniform)
            //     .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications);
            // combatRoom.AddExtraReward(base.Owner, new CardReward(options, 1, base.Owner));
        }

        return Task.CompletedTask;
    }
    //
    // /// <summary>
    // /// 将打出的牌复制加入卡牌奖励选项。
    // /// </summary>
    // public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> options,
    //     CardCreationOptions creationOptions)
    // {
    //     if (player != base.Owner) return false;
    //     if (_rewardedCardInstance.Count <= 0) return false;
    //
    //     return false;
    // }
    //
    //
    // public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    // {
    //     if (player != base.Owner)
    //     {
    //         return false;
    //     }
    //
    //     if (room == null)
    //     {
    //         return false;
    //     }
    //
    //     if (_rewardedCardInstance.Count <= 0) return false;
    //
    //     Status = RelicStatus.Normal;
    //     var cards = _rewardedCardInstance.Where(x => x != null).Cast<CardModel>()
    //         .Select(x => ModelDb.GetById<CardModel>(x.Id)).ToList();
    //
    //     _selectedCardInstance.Clear();
    //     _rewardedCardInstance.Clear();
    //
    //     if (cards.Count <= 0) return false;
    //     var options = new CardCreationOptions(cards, CardCreationSource.Other, CardRarityOddsType.Uniform)
    //         .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications);
    //     rewards.Add(new CardReward(options, cards.Count, player));
    //     Flash();
    //
    //     return true;
    // }
}