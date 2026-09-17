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

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 复甦之梦：接下来3场战斗中的敌人将只有1点生命。
/// 剩余场次显示为遗物计数，用尽后不再触发。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class RevivalDream : TouhouAncientRelics
{
    /// <summary>剩余生效场次。</summary>
    private int _combatsRemaining;

    /// <summary>当前这场战斗是否生效（供战斗中新增敌人时复用判断）。</summary>
    private bool _activeThisCombat;

    private bool _initialized;

    public override bool IsUsedUp => _combatsRemaining <= 0;

    public override bool ShowCounter => !IsUsedUp;

    public override int DisplayAmount => _combatsRemaining;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Combats", 3),
        new DynamicVar("Hp", 1)
    ];

    /// <summary>
    /// 首次取用时把剩余场次初始化为配置值（DynamicVars 在克隆后可用）。
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized) return;
        _combatsRemaining = base.DynamicVars["Combats"].IntValue;
        _initialized = true;
    }

    public override async Task BeforeCombatStart()
    {
        EnsureInitialized();

        _activeThisCombat = _combatsRemaining > 0;
        if (!_activeThisCombat) return;

        await ApplyToEnemies();
    }

    /// <summary>战斗中后续加入的敌人同样处理（如事件/召唤物）。</summary>
    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (!_activeThisCombat) return;
        if (creature.Side != CombatSide.Enemy) return;

        Flash();
        await CreatureCmd.SetCurrentHp(creature, base.DynamicVars["Hp"].BaseValue);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        EnsureInitialized();

        if (_activeThisCombat)
        {
            _combatsRemaining--;
            if (_combatsRemaining < 0) _combatsRemaining = 0;
        }

        _activeThisCombat = false;
        base.Status = IsUsedUp ? RelicStatus.Disabled : RelicStatus.Normal;
        InvokeDisplayAmountChanged();

        return Task.CompletedTask;
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
