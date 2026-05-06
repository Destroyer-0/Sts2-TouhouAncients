using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class Geishehuaxiaojie : TouhouAncientRelics
{
    private int _triggerCount;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("StrengthLose",9)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Creature.Side && combatState.RoundNumber <= 1)
        {
            _triggerCount = 0;
            Flash();
            await PowerCmd.Apply<StrengthPower>(combatState.HittableEnemies, -base.DynamicVars["StrengthLose"].BaseValue, null, null);
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (cardPlay.Card.CombatState == null) return;
        if (_triggerCount >= base.DynamicVars["StrengthLose"].BaseValue) return;

        _triggerCount++;
        await PowerCmd.Apply<StrengthPower>(cardPlay.Card.CombatState.HittableEnemies, 1m, null, null);
    }
}