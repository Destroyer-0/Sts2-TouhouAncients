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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 燎焰之梦：在每场战斗开始时，将1张至亮之焰放入你的手牌，其拥有保留、消耗。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BlazingFlameDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<BrightestFlame>(true)
            .Append(HoverTipFactory.FromKeyword(CardKeyword.Retain))
            .Append(HoverTipFactory.FromKeyword(CardKeyword.Exhaust));

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (player.Creature.CombatState?.RoundNumber != 1) return;

        var amount = base.DynamicVars.Cards.IntValue;
        if (amount <= 0) return;

        Flash();

        var cards = new List<CardModel>();
        for (int i = 0; i < amount; i++)
        {
            var card = player.Creature.CombatState.CreateCard<BrightestFlame>(player);
            // 先赋予关键词再加入手牌，避免玩家看到未附带关键词的牌
            CardCmd.ApplyKeyword(card, CardKeyword.Retain, CardKeyword.Exhaust);
            cards.Add(card);
        }

        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, creator: base.Owner);
    }
}
