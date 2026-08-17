using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 龙颈之玉：蓬莱谜题（类型 0）。5 费。
/// 每打出一张牌，本回合耗能减少 1（可叠加至 0 费）。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class DragonNeckJewelCard : HouraiPuzzleCard
{
    private const int energyCost = 5;

    public DragonNeckJewelCard() : base(energyCost)
    {
    }

    protected override int PuzzleType => 0;

    /// <summary>
    /// 进入战斗时，根据本回合已打出的牌数同步减少耗能（保持与打出记录一致）。
    /// </summary>
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card != this || base.IsClone)
        {
            return Task.CompletedTask;
        }
        int playedThisTurn = CombatManager.Instance.History.CardPlaysFinished.Count(
            (CardPlayFinishedEntry e) => e.CardPlay.Card.Owner == base.Owner && e.HappenedThisTurn(base.CombatState));
        ReduceCostBy(playedThisTurn);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 每打出一张牌，本回合耗能减少 1（可叠加至 0 费）。
    /// </summary>
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner)
        {
            return Task.CompletedTask;
        }
        ReduceCostBy(1);
        return Task.CompletedTask;
    }

    private void ReduceCostBy(int amount)
    {
        base.EnergyCost.AddThisTurn(-amount);
    }
}
