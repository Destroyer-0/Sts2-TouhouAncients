using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 邪仙发簪：每场战斗你首次死亡时，回复到最大生命值的{Percent}%，下个回合开始时，由霍青娥接管你的回合。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class WickedHermitHairpin : TouhouAncientRelics
{
    /// <summary>本场战斗是否已经触发过死亡拦截。</summary>
    private bool _usedThisCombat;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Percent", 40m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(
            new LocString("relics", Id.Entry + ".seigatitle"),
            Description)
    ];

    private LocString Description
    {
        get
        {
            var locString = new LocString("relics", Id.Entry + ".seigadescription");
            locString.Add("energyPrefix", EnergyIconHelper.GetPrefix(this));
            return locString;
        }
    }

    /// <summary>
    /// 每场战斗首次死亡时阻止死亡，触发 <see cref="AfterPreventingDeath"/>。
    /// </summary>
    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != base.Owner.Creature) return true;
        if (_usedThisCombat) return true;   // 本场已用过，正常死亡
        return false;                       // 首次死亡，阻止
    }

    /// <summary>
    /// 阻止死亡后：回复到最大生命值的40%，并为玩家施加"走火入魔"。
    /// </summary>
    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != base.Owner.Creature) return;
        if (_usedThisCombat) return;

        Flash();
        _usedThisCombat = true;

        // 回复到最大生命值的 {Percent}%
        var healPercent = base.DynamicVars["Percent"].BaseValue / 100m;
        var healAmount = (decimal)creature.MaxHp * healPercent;
        await CreatureCmd.Heal(creature, healAmount);

        // 施加"走火入魔"：下个回合开始，霍青娥接管玩家的回合
        await PowerCmd.Apply<PossessedBySeigaPower>(
            new ThrowingPlayerChoiceContext(),
            creature,
            1m,
            creature,
            null);
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }
}
