using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 古代标本：精英敌人的生命值减少20%（向上取整，最终不低于1）。
/// 以「当前最大生命」为基准，多人各自持有时会依次乘算（80% → 64% → 51.2%…）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class AncientSpecimen : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Percent", 20m)];

    /// <summary>开场已在场的敌人走这里：AfterCreatureAddedToCombat 只派发给战斗中途加入的生物。</summary>
    public override async Task BeforeCombatStart()
    {
        var enemies = base.Owner?.Creature?.CombatState?.HittableEnemies;
        if (enemies == null) return;

        foreach (var enemy in enemies) await Reduce(enemy);
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature) => await Reduce(creature);

    private async Task Reduce(Creature creature)
    {
        if (!creature.IsEnemy) return;
        if (!IsEliteCombat()) return;

        var factor = 1m - base.DynamicVars["Percent"].BaseValue / 100m;
        // 基准是当前 MaxHp 而非初始值：前一个持有者改过之后本份继续乘，即依次乘算
        var reduced = Math.Max(1m, Math.Ceiling(creature.MaxHp * factor));
        if (reduced >= creature.MaxHp) return;

        Flash();
        await CreatureCmd.SetCurrentHp(creature, reduced);
    }

    /// <summary>
    /// 精英判定先看地图节点类型：自定义/其他 Mod 的精英遭遇，<c>Encounter.RoomType</c> 可能报 Monster，
    /// 只查 <c>CurrentRoom.RoomType</c> 会漏判（SmokedFan / JyoonFan 同样用节点类型）。
    /// </summary>
    private bool IsEliteCombat()
    {
        var runState = base.Owner?.RunState;
        if (runState == null) return false;

        if (runState.CurrentMapPoint?.PointType == MapPointType.Elite) return true;
        return runState.CurrentRoom is CombatRoom { RoomType: RoomType.Elite };
    }
}
