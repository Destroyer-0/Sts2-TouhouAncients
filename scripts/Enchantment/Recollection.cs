using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 回忆：首次进入弃牌堆时（非正常打出），返回手牌，本回合免费。
/// 仅在回合结束弃牌阶段被弃置时暂存，回合结束时回手并免费。
/// </summary>
public class Recollection : TouhouAncientEnchantmentModel
{
    private CardModel? _pendingRecall;
    private bool isWaitingForFree;

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (!HasCard) return;
        if (card != base.Card) return;
        if (base.Status != EnchantmentStatus.Normal) return;

        if (card.Pile?.Type == PileType.Discard)
        {
            base.Status = EnchantmentStatus.Disabled;
            await CardCmd.AutoPlay(
                new BlockingPlayerChoiceContext(),
                card,
                target: null
            );
            //isWaitingForFree = false;
        }
        // else
        // {
        //     // 回合结束弃牌阶段 → 记录，等下回合开始
        //     _pendingRecall = card;
        // }
    }

    // public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    // {
    //     if (!HasCard) return;
    //     if (card != base.Card) return;
    //     if (base.Status != EnchantmentStatus.Normal) return;
    //     
    //     //if (!CombatManager.Instance.EndingPlayerTurnPhaseTwo)
    //     {
    //         await CardPileCmd.Add(card, PileType.Hand);
    //         card.SetStarCostUntilPlayed(0);
    //         card.EnergyCost.SetUntilPlayed(0);
    //         base.Status = EnchantmentStatus.Disabled;
    //     }
    // }

    // public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    // {
    //     if (side != CombatSide.Player) return; // 只处理玩家回合结束
    //     if (!HasCard) return;
    //     if (base.Status != EnchantmentStatus.Normal) return;
    //     if (_pendingRecall != base.Card) return;
    //     if (base.Card.Pile?.Type != PileType.Discard) return;
    //
    //     base.Status = EnchantmentStatus.Disabled;
    //     await CardPileCmd.Add(base.Card, PileType.Hand);
    //     base.Card.SetToFreeThisTurn();
    //     _pendingRecall = null;
    // }
}