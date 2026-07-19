using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 百万英镑：拾起时，将一张疫病支票加入你的牌组。
/// 疫病支票：1费，诅咒，保留。抽到时耗能减少1。
/// 在手牌中时，耗能高于此牌的卡牌耗能减少1，不能打出耗能低于此牌的卡牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MillionPounds : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromCard<TheMillionPoundNote>() };

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != base.Owner) return;
        if (player.Creature.CombatState?.RoundNumber != 1) return;

        Flash();
        var note = player.Creature.CombatState.CreateCard<TheMillionPoundNote>(player);
        await CardPileCmd.AddGeneratedCardsToCombat([note], PileType.Hand, creator: base.Owner);
    }
}