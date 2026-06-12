using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 天启万象：
/// 在你的第三回合开始时，翻倍你的所有正面状态。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class HeavenlyRevelation : TouhouAncientRelics
{
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != base.Owner.Creature.Side) return;
        if (combatState.RoundNumber != 3) return;

        Flash();

        var creature = base.Owner.Creature;
        var buffs = creature.Powers
            .Where(p => p is { Type: PowerType.Buff, Amount: > 0 ,StackType: PowerStackType.Counter })
            .ToList();

        await PlayerCmd.GainStars(Owner.PlayerCombatState.Stars, Owner);
        foreach (var buff in buffs)
        {
            await PowerCmd.ModifyAmount(buff, buff.Amount, creature, null);
        }
    }
}
