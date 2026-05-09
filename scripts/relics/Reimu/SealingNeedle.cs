using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 封魔针：战斗开始时，为所有敌人附加1层虚弱。你对处于虚弱状态的敌人造成伤害+3。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class SealingNeedle : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ExtraDamage", 3)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>()
    ];

    /// <summary>
    /// 战斗开始时，为所有敌人附加1层虚弱。
    /// </summary>
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        if (side != base.Owner.Creature.Side) return;
        if (combatState.RoundNumber > 1) return;

        Flash();
        await PowerCmd.Apply<WeakPower>(combatState.HittableEnemies, 1m, base.Owner.Creature, null);
    }

    /// <summary>
    /// 对处于虚弱状态的敌人造成额外伤害。
    /// </summary>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != base.Owner?.Creature) return 0m;
        if (target == null || !target.IsAlive) return 0m;
        if (!target.HasPower<WeakPower>()) return 0m;
        if (!props.IsPoweredAttack()) return 0m;

        return base.DynamicVars["ExtraDamage"].BaseValue;
    }
}
