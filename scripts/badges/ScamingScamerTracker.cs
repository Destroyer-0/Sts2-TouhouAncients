using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.encounters;

namespace TouhouAncients.Scripts.badges;

/// <summary>依神姐妹战斗胜利时，身上王国资产仍大于 0 的玩家置位。</summary>
public class ScamingScamerTracker : BadgeModel
{
    // 王国资产在 Player.AfterCombatEnd 里被清空，只能抢在那之前（Hook.AfterCombatEnd）判定
    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (room.Encounter is not YorigamiSistersEncounter) return Task.CompletedTask;
        if (!room.Enemies.All(enemy => enemy.IsDead)) return Task.CompletedTask;

        foreach (var player in room.CombatState.Players)
        {
            if (player.Creature.GetPowerAmount<RoyaltiesPower>() > 0)
            {
                ScamingScamer.Unlock(player);
            }
        }

        return Task.CompletedTask;
    }
}
