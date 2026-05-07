using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class NobleBrooch : TouhouAncientRelics
{
    private const int MaxPerRound = 2;
    private const int MaxTotal = 4;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        var rng = player.RunState.Rng.CombatCardGeneration;
        var hand = PileType.Hand.GetPile(player);
        if (hand.Cards.Count <= 0) return;

        Flash();

        // Round 1: 选择至多2张牌变化
        var firstSelected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1, MaxPerRound),
            context: choiceContext,
            player: player,
            filter: null,
            source: this)).ToList();

        if (firstSelected.Count <= 0) return;

        var transformedThisTurn = new HashSet<CardModel>();
        int totalTransformed = 0;

        foreach (var card in firstSelected)
        {
            var result = await CardCmd.TransformToRandom(card, rng);
            if (result.success)
            {
                transformedThisTurn.Add(result.cardAdded);
                totalTransformed++;
            }
        }

        // Round 2: 再次变化以此法变化的牌（至多满4次）
        int remaining = MaxTotal - totalTransformed;
        while (remaining > 0 && transformedThisTurn.Count > 0)
        {
            var eligible = transformedThisTurn
                .Where(c => c.Pile?.Type == PileType.Hand)
                .ToList();
            if (eligible.Count <= 0) break;

            int roundMax = MaxPerRound < remaining ? MaxPerRound : remaining;
            var nextSelected = (await CardSelectCmd.FromHand(
                prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1, roundMax),
                context: choiceContext,
                player: player,
                filter: c => eligible.Contains(c),
                source: this)).ToList();

            if (nextSelected.Count <= 0) break;

            transformedThisTurn.Clear();
            foreach (var card in nextSelected)
            {
                var result = await CardCmd.TransformToRandom(card, rng);
                if (result.success)
                {
                    transformedThisTurn.Add(result.cardAdded);
                    totalTransformed++;
                }
            }

            remaining = MaxTotal - totalTransformed;
        }
    }
}
