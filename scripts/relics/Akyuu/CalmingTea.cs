using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 宁神红茶：回合结束时，若你拥有负面状态（减益能力），获得5点格挡。
/// 格挡在敌人回合开始时才清除，所以回合结束时获得的格挡能挡住敌人的攻击。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class CalmingTea : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4m, ValueProp.Move)];

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != base.Owner.Creature.Side) return;
        if (base.Owner.Creature.Powers.All(p => p.Type != PowerType.Debuff)) return;

        Flash();
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, null);
    }
}
