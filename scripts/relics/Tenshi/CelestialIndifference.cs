using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 天界冷漠：回合结束时，升级你本回合获得过的牌与牌堆顶的牌。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class CelestialIndifference : TouhouAncientRelics
{
    private readonly HashSet<CardModel> _cardsGainedThisTurn = new();

    public override Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
    {
        if (card.Owner != base.Owner) return Task.CompletedTask;
        _cardsGainedThisTurn.Add(card);
        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Owner != base.Owner) return Task.CompletedTask;
        if (base.Owner.PlayerCombatState == null) return Task.CompletedTask;
        
        if (card.Pile!=null&& card.Pile.Type is PileType.Hand)
        {
            _cardsGainedThisTurn.Add(card);
        }

        return Task.CompletedTask;
    }
    
    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Creature.Side) return;
        if (base.Owner.PlayerCombatState == null) return;

        var combatState = base.Owner.PlayerCombatState;

        bool anyUpgraded = false;

        foreach (var card in _cardsGainedThisTurn.ToList())
        {
            if (card.HasBeenRemovedFromState) continue;
            if (!card.IsInCombat) continue;
            if (card.IsUpgradable)
            {
                CardCmd.Upgrade(card);
                anyUpgraded = true;
            }
        }

        var drawPile = combatState.DrawPile;
        if (!drawPile.IsEmpty)
        {
            var topCard = drawPile.Cards[0];
            if (topCard.IsUpgradable)
            {
                CardCmd.Upgrade(topCard);
                anyUpgraded = true;
            }
        }

        _cardsGainedThisTurn.Clear();

        if (anyUpgraded)
        {
            Flash();
        }
    }
}
