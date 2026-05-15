using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 天赐甲胄：在每个回合开始时获得{Energy:energyIcons()}。每回合第一次从卡牌中获得的格挡值减半。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HolyArmor : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.ForEnergy(this),
            HoverTipFactory.Static(StaticHoverTip.Block),
        ];

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Creature.Side)
        {
            Flash();
            await PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, base.Owner);
        }
    }

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != base.Owner.Creature) return 1m;
        if (!props.IsCardOrMonsterMove()) return 1m;
        if (cardSource == null) return 1m;

        // 检查本回合是否已经通过卡牌获得过格挡（排除当前这次）
        int priorBlockGains = CombatManager.Instance.History.Entries
            .OfType<BlockGainedEntry>()
            .Count(e => e.HappenedThisTurn(base.Owner.Creature.CombatState)
                        && e.Actor == target
                        && e.Props.IsCardOrMonsterMove()
                        && e.CardPlay != cardPlay);

        if (priorBlockGains >= 1) return 1m;
        Flash();
        return 0.5m;
    }
}
