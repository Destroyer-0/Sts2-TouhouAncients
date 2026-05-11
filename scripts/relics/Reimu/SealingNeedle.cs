using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 封魔针：你造成伤害时，给予1虚弱。你对处于虚弱状态的敌人造成伤害增加等同于其虚弱层数的伤害。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class SealingNeedle : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>()
    ];

    /// <summary>
    /// 造成伤害后，给予目标1层虚弱（可叠加）。
    /// </summary>
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != base.Owner?.Creature) return;
        if (!target.IsAlive || !target.IsEnemy) return;

        await PowerCmd.Apply<WeakPower>(target, 1m, dealer, cardSource);
    }

    /// <summary>
    /// 对处于虚弱状态的敌人，增加等同于其虚弱层数的伤害值。
    /// </summary>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != base.Owner?.Creature) return 0m;
        if (cardSource == null) return 0m;
        if (target == null || !target.IsAlive || !target.IsEnemy) return 0m;
        if (!target.HasPower<WeakPower>()) return 0m;

        return Math.Floor(target.GetPowerAmount<WeakPower>() / 2m);
    }
}