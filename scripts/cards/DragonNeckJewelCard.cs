using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 龙颈之玉：蓬莱谜题（类型 0）。5 费。
/// 每打出一张牌，本回合耗能减少{CostReduce}（可叠加至 0 费）。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class DragonNeckJewelCard : HouraiPuzzleCard
{
    private const int energyCost = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("CostReduce", 1m)];

    public DragonNeckJewelCard() : base(energyCost)
    {
    }

    protected override PuzzleCardName PuzzleType => PuzzleCardName.龙颈之玉;

    /// <summary>
    /// 每打出一张牌，本回合耗能减少{CostReduce}（可叠加至 0 费）。
    /// </summary>
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner)
        {
            return Task.CompletedTask;
        }
        ReduceCostBy(base.DynamicVars["CostReduce"].IntValue);
        return Task.CompletedTask;
    }

    private void ReduceCostBy(int amount)
    {
        base.EnergyCost.AddThisTurn(-amount);
    }
}
