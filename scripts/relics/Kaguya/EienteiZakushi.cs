using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 永远亭座药：拾起时，用随机药水填满你的药水栏位。
/// 每回合开始时，你每拥有一瓶药水，对随机一个敌人造成8点伤害。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class EienteiZakushi : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DamagePerPotion", 8m),
    ];

    /// <summary>
    /// 拾起时，用随机药水填满药水栏位。
    /// </summary>
    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (!player.HasOpenPotionSlots) return;

        var emptyCount = player.MaxPotionCount - player.Potions.Count();
        var potions = PotionFactory.CreateRandomPotionsOutOfCombat(player, emptyCount, player.RunState.Rng.CombatPotionGeneration);
        foreach (var potion in potions)
        {
            await PotionCmd.TryToProcure(potion.ToMutable(), player);
        }
    }

    /// <summary>
    /// 每回合开始时，每拥有一瓶药水对随机敌人造成8点伤害。
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        var potionCount = player.Potions.Count();
        if (potionCount <= 0) return;

        var enemies = player.Creature.CombatState.GetOpponentsOf(player.Creature)
            .Where(e => e.IsAlive)
            .ToList();

        if (enemies.Count == 0) return;

        Flash();

        Color color = new Color("FFFFFF80");
        double num2 = ((SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast) ? 0.2 : 0.3);
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NHorizontalLinesVfx.Create(color,   (double)Mathf.Min(8, potionCount) * num2));
        for (int i = 0; i < potionCount; i++)
        {
            Creature creature = base.Owner.RunState.Rng.CombatTargets.NextItem(base.Owner.Creature.CombatState.HittableEnemies);
            if (creature != null)
            {
                VfxCmd.PlayOnCreatureCenter(creature, "vfx/vfx_attack_blunt");
                await CreatureCmd.Damage(choiceContext, creature, base.DynamicVars["DamagePerPotion"].IntValue, ValueProp.Unpowered, base.Owner.Creature);
            }
            //await CreatureCmd.Damage(choiceContext, creature, DamagePerPotion, this);
        }
    }
}
