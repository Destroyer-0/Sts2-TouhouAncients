using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 人魂灯：有任意角色的生命值低于50%时，获得1死神形态，每场战斗只触发一次。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class SoulLattern : TouhouAncientRelics
{
    private bool hasTriggeredThisCombat = false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ReaperFormAmount", 1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ReaperFormPower>()];

    /// <summary>
    /// 战斗开始时检查一次（需要全扫）
    /// </summary>
    public override async Task BeforeCombatStartLate()
    {
        if (hasTriggeredThisCombat) return;
        if (base.Owner?.Creature == null) return;

        var combatState = base.Owner.Creature.CombatState;
        if (combatState == null) return;

        var allCreatures = combatState.GetCreaturesOnSide(base.Owner.Creature.Side)
            .Concat(combatState.GetOpponentsOf(base.Owner.Creature))
            .Where(c => c.IsAlive);

        var playerCombatState = base.Owner.PlayerCombatState;
        if (playerCombatState?.Pets != null)
        {
            allCreatures = allCreatures.Concat(playerCombatState.Pets.Where(p => p.IsAlive));
        }

        if (allCreatures.Any(c => c.CurrentHp < c.MaxHp * 0.5m))
            await Trigger();
    }

    /// <summary>
    /// 任意单位（含宠物）生命值降低时，检查该单位 HP 是否低于 50%
    /// </summary>
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (hasTriggeredThisCombat) return;
        if (delta >= 0m) return;
        if (!creature.IsAlive || creature.CurrentHp < creature.MaxHp * 0.5m)
            await Trigger();
    }

    private async Task Trigger()
    {
        if (hasTriggeredThisCombat) return;
        Flash();
        hasTriggeredThisCombat = true;
        await PowerCmd.Apply<ReaperFormPower>(base.Owner.Creature, base.DynamicVars["ReaperFormAmount"].BaseValue,
            base.Owner.Creature, null);
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        hasTriggeredThisCombat = false;
        return Task.CompletedTask;
    }
}