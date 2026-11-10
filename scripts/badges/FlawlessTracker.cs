using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.encounters;
using TouhouAncients.Scripts.monsters;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.badges;

/// <summary>纯狐战斗胜利时，绀珠悯悼层数未减（一次复活都没触发）的玩家置位。</summary>
public class FlawlessTracker : BadgeModel
{
    // 绀珠悯悼在 Player.AfterCombatEnd 里随 Power 一起清空，只能抢在那之前（Hook.AfterCombatEnd）判定
    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (room.Encounter is not JunkoEncounter) return Task.CompletedTask;
        if (!room.Enemies.All(enemy => enemy.IsDead)) return Task.CompletedTask;

        foreach (var player in room.CombatState.Players)
        {
            // 层数只在复活时递减，未减即一次都没触发；>= 而非 == 是为了容忍将来出现加层来源
            var protection = player.Creature.GetPower<UltramarineOrbProtectionPower>();
            if (protection == null || protection.Amount >= JunkoMonster.ProtectionLayers)
            {
                Flawless.Unlock(player);
            }
        }

        return Task.CompletedTask;
    }
}
