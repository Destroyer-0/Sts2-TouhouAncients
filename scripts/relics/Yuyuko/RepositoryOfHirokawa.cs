using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class RepositoryOfHirokawa : TouhouAncientRelics
{
    private HashSet<bool> playedPowerThisTurn = new();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BufferAmount", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BufferPower>()];


    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return;
        if (cardPlay.Card.Type != CardType.Power)
        {
            playedPowerThisTurn.Add(false);
        }
        else
        {
            playedPowerThisTurn.Add(true);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Creature.Side) return;
        if (playedPowerThisTurn.Count > 0 && playedPowerThisTurn.All(x => x))
        {
            Flash();
            await PowerCmd.Apply<BufferPower>(base.Owner.Creature, base.DynamicVars["BufferAmount"].BaseValue,
                base.Owner.Creature, null);
        }

        playedPowerThisTurn.Clear();
    }
}