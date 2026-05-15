using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 天宇诏令：拾起时，获得天赋君权。查看15张来自储君的升级过的牌，选择任意数量加入牌组。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class CosmicDecree : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(15),
        new StringVar("Character", ModelDb.Character<Regent>().Title.GetFormattedText())
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromRelic<DivineRight>().Append(HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientRegent));

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        // 获得天赋君权
        await RelicCmd.Obtain<DivineRight>(player);

        // 创建15张储君的升级过的卡牌
        int cardCount = base.DynamicVars.Cards.IntValue;
        var regentCardPool = ModelDb.Character<Regent>().CardPool;
        var options = CardCreationOptions.ForNonCombatWithUniformOdds(
                new[] { regentCardPool },
                _ => true)
            .WithFlags(CardCreationFlags.NoRarityModification | CardCreationFlags.NoCardPoolModifications | CardCreationFlags.NoUpgradeRoll);

        var creationResults = CardFactory.CreateForReward(player, cardCount, options).ToList();

        foreach (var result in creationResults)
        {
            if (result.Card.IsUpgradable)
            {
                CardCmd.Upgrade(result.Card);
            }
        }

        var selected = (await CardSelectCmd.FromSimpleGridForRewards(
            new BlockingPlayerChoiceContext(),
            creationResults,
            player,
            new CardSelectorPrefs(
                RelicModel.L10NLookup(base.Id.Entry + ".selectionScreenPrompt"),
                0,
                creationResults.Count)
        )).ToList();

        var addResults = new List<CardPileAddResult>();
        foreach (var card in selected)
        {
            addResults.Add(await CardPileCmd.Add(card, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd(addResults, 2f);
    }
}
