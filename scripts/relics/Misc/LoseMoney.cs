using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 破财消灾：每当你打出3张牌，消耗4金币获得1能量。
/// 金币不足4时后续效果不触发。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class LoseMoney : TouhouAncientRelics
{
    public override RelicRarity Rarity => RelicRarity.Event;
    [SavedProperty]
    public int CardsPlayed { get; set; }

    private const int CardsNeeded = 3;
    private const int GoldCost = 4;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardsNeeded", CardsNeeded),
        new DynamicVar("GoldCost", GoldCost)
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner || !CombatManager.Instance.IsInProgress)
            return;

        CardsPlayed++;

        if (CardsPlayed >= CardsNeeded)
        {
            CardsPlayed = 0;

            if (base.Owner.Gold >= GoldCost)
            {
                Flash();
                await PlayerCmd.LoseGold(GoldCost, base.Owner, GoldLossType.Spent);
                await PlayerCmd.GainEnergy(1, base.Owner);
            }
        }
    }
}
