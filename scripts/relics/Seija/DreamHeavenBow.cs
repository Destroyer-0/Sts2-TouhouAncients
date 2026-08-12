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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 天壤梦弓：战斗开始时，对所有敌人造成伤害，
/// 对生命值最高的敌人额外造成2次伤害并给予2层虚弱。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DreamHeavenBow : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Damage", 8),
        new DynamicVar("Weak", 2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        var combatState = player.Creature.CombatState;
        if (combatState == null || combatState.RoundNumber != 1) return;

        var enemies = combatState.HittableEnemies;
        if (enemies.Count <= 0) return;

        Flash();

        var baseDamage = base.DynamicVars["Damage"].BaseValue;


        // 找到生命值最高的敌人（取所有并列最高 CurrentHp 的敌人）
        var maxHp = enemies.Max(e => e.CurrentHp);
        var highestHpEnemies = enemies.Where(e => e.CurrentHp == maxHp).ToList();

        // 对所有敌人造成基础伤害
        VfxCmd.PlayOnCreatureCenters(enemies, "vfx/vfx_attack_slash");
        await CreatureCmd.Damage(choiceContext, enemies, baseDamage, ValueProp.Unpowered, base.Owner.Creature);

        // 对生命值最高的敌人：1次基础伤害 + 额外2次伤害 = 共3次
        for (var index = 0; index < 2; index++)
        {
            await CreatureCmd.Damage(choiceContext, highestHpEnemies, baseDamage, ValueProp.Unpowered, base.Owner.Creature);
        }

        await PowerCmd.Apply<WeakPower>(choiceContext, highestHpEnemies, base.DynamicVars["Weak"].BaseValue, base.Owner.Creature, null);
    }
}