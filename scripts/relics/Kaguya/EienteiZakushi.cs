using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 永远亭座药：拾起时，用随机药水填满你的药水栏位。
/// 每回合开始时，你每拥有一瓶药水，对随机一个敌人造成8点伤害。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class EienteiZakushi : TouhouAncientRelics
{
    private const decimal DamagePerPotion = 8m;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DamagePerPotion", DamagePerPotion),
    ];

    /// <summary>
    /// 拾起时，用随机药水填满药水栏位。
    /// </summary>
    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player == null) return;
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

        for (int i = 0; i < potionCount; i++)
        {
            var target = enemies[player.RunState.Rng.Shuffle.Next(enemies.Count)];
            await CreatureCmd.Damage(choiceContext, target, DamagePerPotion, this);
        }
    }
}
