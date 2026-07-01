using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 绯红晶石：
/// 在你的回合开始时，如果你的生命值不低于50%，获得1能量。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class CrimsonCrystal : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HpThreshold", 50),
        new EnergyVar(1)
    ];

    
    public override Task BeforeCombatStart()
    {
        if (CanGetEnergy())
        {
            base.Status = RelicStatus.Active;
        }
        return Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != base.Owner.Creature)
        {
            return Task.CompletedTask;
        }
        if (!CombatManager.Instance.IsInProgress)
        {
            return Task.CompletedTask;
        }
        base.Status = (CanGetEnergy() ? RelicStatus.Active : RelicStatus.Normal);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        base.Status =  RelicStatus.Normal;
        return base.AfterCombatEnd(room);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        var creature = player.Creature;
        if (creature.MaxHp <= 0) return;

        if (CanGetEnergy())
        {
            Flash();
            await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, player);
        }
    }

    private bool CanGetEnergy() => Owner.Creature.CurrentHp * 100m / Owner.Creature.MaxHp>= base.DynamicVars["HpThreshold"].BaseValue;
}