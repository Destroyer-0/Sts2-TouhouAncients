using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 折叠伞：战斗开始时，获得5层倒映（ReflectPower）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class FoldingUmbrella : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ReflectPower>()];


    public override async Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == Owner.Creature.Side && participants.Contains(Owner.Creature))
        {
            Flash();
            await PowerCmd.Apply<ReflectPower>(new ThrowingPlayerChoiceContext(), base.Owner.Creature, 5m,
                base.Owner.Creature, null);
        }
    }
}