using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
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
    private IEnumerable<Creature> Enemies => base.Owner.Creature.CombatState
        .GetOpponentsOf(base.Owner.Creature)
        .Where(c => c.IsAlive);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task BeforeCombatStart()
    {
        _triggerCount = 0;
        Flash();
        await PowerCmd.Apply<StrengthPower>(Enemies, -base.DynamicVars["StrengthLose"].BaseValue, null, null);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (_triggerCount >= base.DynamicVars["StrengthLose"].BaseValue) return;

        _triggerCount++;
        await PowerCmd.Apply<StrengthPower>(Enemies, 1m, null, null);
    }
}