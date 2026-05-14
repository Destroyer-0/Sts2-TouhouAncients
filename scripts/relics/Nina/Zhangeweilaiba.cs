using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class Zhangeweilaiba : TouhouAncientRelics
{
    // 遗物的数值。替换本地化中的{Cards}。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Increase", 2)];

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner)
        {
            return Task.CompletedTask;
        }
        if (!cardPlay.Card.EnergyCost.CostsX && !cardPlay.Card.HasStarCostX)
        {
            return Task.CompletedTask;
        }
        Flash();
        return Task.CompletedTask;
    }

    public override int ModifyXValue(CardModel card, int originalValue)
    {
        if (base.Owner != card.Owner)
        {
            return originalValue;
        }
        return originalValue + base.DynamicVars["Increase"].IntValue;
    }

    public override async Task AfterObtained()
    {
        Player owner = Owner;
        CardCreationOptions options = CardCreationOptions.ForNonCombatWithUniformOdds([owner.Character.CardPool], (CardModel c) => c.EnergyCost.CostsX || c.HasStarCostX).WithFlags(CardCreationFlags.NoRarityModification | CardCreationFlags.NoUpgradeRoll);
        List<CardCreationResult> list = CardFactory.CreateForReward(base.Owner, options.GetPossibleCards(owner).Count(), options).ToList();
        foreach (CardModel item in await
                     CardSelectCmd.FromSimpleGridForRewards(
                         prefs: new CardSelectorPrefs(
                             RelicModel.L10NLookup(base.Id.Entry + ".selectionScreenPrompt"),
                             1,
                            1
                             ), context: new BlockingPlayerChoiceContext(), cards: list, player: base.Owner))
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(item, PileType.Deck));
        }
    }

    // public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    // {
    //     // 这里的DynamicVars.Cards.IntValue为上面设置的CardsVar的数值。
    //     await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, player);
    // }
}