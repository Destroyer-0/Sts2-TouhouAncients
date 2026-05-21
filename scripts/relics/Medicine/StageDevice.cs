using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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
/// 舞台装置：
/// 你的回合开始时，给予所有敌人1层易伤，如果已存在拥有易伤的敌人，失去1点生命。
/// 你的回合结束时，给予所有敌人1层虚弱，如果已存在拥有虚弱的敌人，失去1点生命。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class StageDevice : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("VulnAmount", 1),
        new DynamicVar("WeakAmount", 1),
        new HpLossVar(1)
    ];


    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        Flash();

        var enemies = player.Creature.CombatState?.GetOpponentsOf(player.Creature).Where(c => c.IsAlive).ToList();
        if (enemies == null || enemies.Count == 0) return;

        // 检查是否已有易伤的敌人
        var anyVulnerable = enemies.Any(e => e.HasPower<VulnerablePower>());

        // 给予所有敌人 VulnAmount 层易伤
        await PowerCmd.Apply<VulnerablePower>(enemies, base.DynamicVars["VulnAmount"].BaseValue, player.Creature, null);

        // 如果已有易伤的敌人，失去 StartHpLoss 点生命
        if (anyVulnerable)
        {
            await CreatureCmd.Damage(choiceContext, base.Owner.Creature, base.DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, base.Owner.Creature);
            SfxCmd.Play("event:/sfx/characters/ironclad/ironclad_bloodwall");
        }
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Creature.Side) return;

        Flash();

        var player = base.Owner;
        var enemies = player.Creature.CombatState?.GetOpponentsOf(player.Creature).Where(c => c.IsAlive).ToList();
        if (enemies == null || enemies.Count == 0) return;

        // 检查是否已有虚弱的敌人
        var anyWeak = enemies.Any(e => e.HasPower<WeakPower>());

        // 给予所有敌人 WeakAmount 层虚弱
        await PowerCmd.Apply<WeakPower>(enemies, base.DynamicVars["WeakAmount"].BaseValue, player.Creature, null);

        // 如果已有虚弱的敌人，失去 EndHpLoss 点生命
        if (anyWeak)
        {
            await CreatureCmd.Damage(choiceContext, base.Owner.Creature, base.DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, base.Owner.Creature);
            SfxCmd.Play("event:/sfx/characters/ironclad/ironclad_bloodwall");
        }
    }
}