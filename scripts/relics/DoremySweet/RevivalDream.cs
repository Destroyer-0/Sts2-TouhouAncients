using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 复甦之梦：接下来3场战斗中的敌人将只有1点生命。
/// 已生效场次用 <c>[SavedProperty]</c> 保存（参照原版「羽翼之靴」/本 Mod「二重结界」），
/// 读档后剩余场次照旧，不会重新回满；用尽后遗物永久失效。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class RevivalDream : TouhouAncientRelics
{
    /// <summary>生效场次上限。</summary>
    private const int CombatCount = 3;

    /// <summary>描述与计数器共用的 DynamicVar 键。</summary>
    private const string CombatsKey = "Combats";

    /// <summary>已生效的战斗场次。</summary>
    private int _combatsUsed;

    /// <summary>当前这场战斗是否生效（供战斗中新增敌人时复用判断）。</summary>
    private bool _activeThisCombat;

    /// <summary>
    /// 已生效场次：随存档保存，读档时由 setter 恢复剩余场次、计数器与失效状态。
    /// </summary>
    [SavedProperty]
    public int TouhouAncients_CombatsUsed
    {
        get => _combatsUsed;
        set
        {
            AssertMutable();
            _combatsUsed = Math.Clamp(value, 0, CombatCount);
            base.DynamicVars[CombatsKey].BaseValue = CombatCount - _combatsUsed;
            InvokeDisplayAmountChanged();
            if (IsUsedUp) base.Status = RelicStatus.Disabled;
        }
    }

    public override bool IsUsedUp => _combatsUsed >= CombatCount;

    public override bool ShowCounter => !IsUsedUp;

    public override int DisplayAmount => CombatCount - _combatsUsed;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(CombatsKey, CombatCount),
        new DynamicVar("Hp", 1)
    ];

    public override async Task BeforeCombatStart()
    {
        _activeThisCombat = !IsUsedUp;
        if (!_activeThisCombat) return;

        await ApplyToEnemies();
        if (_activeThisCombat)
        {
            TouhouAncients_CombatsUsed++;
        }
    }

    /// <summary>战斗中后续加入的敌人同样处理（如事件/召唤物）。</summary>
    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (!_activeThisCombat) return;
        if (creature.Side != CombatSide.Enemy) return;

        Flash();
        await CreatureCmd.SetCurrentHp(creature, base.DynamicVars["Hp"].BaseValue);
    }

    private async Task ApplyToEnemies()
    {
        var combatState = base.Owner?.Creature?.CombatState;
        if (combatState == null) return;

        Flash();

        var enemies = combatState.HittableEnemies;
        foreach (var enemy in enemies)
        {
            await CreatureCmd.SetCurrentHp(enemy, base.DynamicVars["Hp"].BaseValue);
        }
    }
}
