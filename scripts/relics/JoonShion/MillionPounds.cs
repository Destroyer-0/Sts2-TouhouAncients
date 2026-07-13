using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 百万英镑：战斗开始时，将一张【疫病支票】加入你的手牌。
/// 疫病支票：2(1)费，消耗，保留。消耗你所有的债务。
/// 只要这张牌在你的手中，你就能免费打出前两张牌，并在回合结束时将一张债务加入抽牌堆。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MillionPounds : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<TheMillionPoundNote>();

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        var combatState = player.Creature.CombatState;
        if (combatState == null || combatState.RoundNumber != 1) return;

        Flash();

        await CardPileCmd.AddGeneratedCardsToCombat([combatState.CreateCard<TheMillionPoundNote>(base.Owner)], PileType.Hand, creator: base.Owner);
    }
}
