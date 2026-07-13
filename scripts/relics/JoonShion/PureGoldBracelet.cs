using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 纯金手镯：每回合额外抽4张牌，弃置额外抽牌中费用低于2的牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class PureGoldBracelet : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(4),
        new DynamicVar("Threshold",2)
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (player.PlayerCombatState == null) return;

        var hand = player.PlayerCombatState.Hand;
        if (hand.IsEmpty) return;

        var cards= await CardPileCmd.Draw(choiceContext, base.DynamicVars["Cards"].IntValue, player, fromHandDraw: true);
        // 弃掉手牌中费用低于2的牌（至多弃ExtraDraw张）
        var lowCostCards = cards
            .Where(c => c.EnergyCost.GetResolved() < base.DynamicVars["Threshold"].IntValue)
            .Take(base.DynamicVars.Cards.IntValue)
            .ToList();

        if (lowCostCards.Count <= 0) return;

        Flash();
        await CardCmd.Discard(choiceContext, lowCostCards);
    }
}
