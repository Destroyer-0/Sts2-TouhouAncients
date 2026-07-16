using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 百万英镑：在每场战斗开始时，获得名流，并将一张疫病支票加入手牌。
/// 名流：每回合你打出的前1张牌会免费打出，并将一张债务加入弃牌堆。
/// 疫病支票：2(1)费，保留，消耗。触发然后消耗你的所有债务，移除名流状态。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MillionPounds : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<TheMillionPoundNotePower>() }.Concat(
            HoverTipFactory.FromCardWithCardHoverTips<TheMillionPoundNote>());

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (player.Creature.CombatState?.RoundNumber != 1) return;

        Flash();
        await PowerCmd.Apply<TheMillionPoundNotePower>(new ThrowingPlayerChoiceContext(), base.Owner.Creature, 1m,
            base.Owner.Creature, null);
        var note = player.Creature.CombatState.CreateCard<TheMillionPoundNote>(player);
        await CardPileCmd.AddGeneratedCardsToCombat([note], PileType.Hand, creator: base.Owner);
    }
}