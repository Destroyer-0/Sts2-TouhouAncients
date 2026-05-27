using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 恶毒的童话书：
/// 在你的回合开始时，获得 Power 点力量。所有敌人初始获得 ThornsAmount 层荆棘。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class MaliciousFairyTale : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Power", 2),
        new DynamicVar("ThornsAmount", 1)
    ];


    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [
            HoverTipFactory.FromPower<StrengthPower>(),
            HoverTipFactory.FromPower<ThornsPower>(),
        ];
    
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (player.Creature.CombatState==null) return;

        Flash();

        // 获得 Power 点力量
        await PowerCmd.Apply<StrengthPower>(player.Creature, base.DynamicVars["Power"].BaseValue, player.Creature, null);
        
        //await PowerCmd.Apply<ThornsPower>(player.Creature.CombatState.Enemies, base.DynamicVars["ThornsAmount"].BaseValue, player.Creature, null);
    }
    
    public override async Task BeforeCombatStartLate()
    {
        var creature = base.Owner.Creature;
        if (creature?.CombatState == null) return;
    
        // 所有敌人获得 ThornsAmount 层荆棘
        var enemies = creature.CombatState.GetOpponentsOf(creature).Where(c => c.IsAlive);
        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<ThornsPower>(enemy, base.DynamicVars["ThornsAmount"].BaseValue, enemy, null);
        }
    }
    
    //
    // public override async Task BeforeCombatStartLate()
    // {
    //     var creature = base.Owner.Creature;
    //     if (creature?.CombatState == null) return;
    //
    //     // 所有敌人获得 ThornsAmount 层荆棘
    //     var enemies = creature.CombatState.GetOpponentsOf(creature).Where(c => c.IsAlive);
    //     foreach (var enemy in enemies)
    //     {
    //         await PowerCmd.Apply<ThornsPower>(enemy, base.DynamicVars["ThornsAmount"].BaseValue, enemy, null);
    //     }
    // }
}
