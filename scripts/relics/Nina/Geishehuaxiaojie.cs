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
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class Geishehuaxiaojie : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("StrengthLose", 8)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    private readonly List<GeishehuaxiaojiejianshaoliliangPower> _powers = new List<GeishehuaxiaojiejianshaoliliangPower>();

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Creature.Side && combatState.RoundNumber <= 1)
        {
            _powers.Clear();
            Flash();
            _powers.AddRange(await PowerCmd.Apply<GeishehuaxiaojiejianshaoliliangPower>(combatState.HittableEnemies, base.DynamicVars["StrengthLose"].BaseValue, null, null));
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (cardPlay.Card.CombatState == null) return;
        if (_powers.Count == 0) return;
        foreach (var power in _powers)
        {
            if (power.Owner == null) continue;
            if (!power.Owner.IsAlive) continue;

            await PowerCmd.ModifyAmount(power, -1, null, null);
            await PowerCmd.Apply<StrengthPower>(power.Owner, 1, null, null);
        }
    }
}