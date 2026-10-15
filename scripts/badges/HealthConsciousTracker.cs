using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.potions;

namespace TouhouAncients.Scripts.badges;

/// <summary>打赢第一幕 Boss 时药水栏里仍有 2 瓶养生汤。</summary>
public class HealthConsciousTracker : BadgeModel
{
    public override Task AfterCombatEnd(CombatRoom room)
    {
        // 第一幕 Boss 战胜利 = 通过第一幕
        if (room.RoomType != RoomType.Boss) return Task.CompletedTask;
        if (room.CombatState.RunState.CurrentActIndex != 0) return Task.CompletedTask;
        if (!room.Enemies.All(enemy => enemy.IsDead)) return Task.CompletedTask;

        ModelId potionId = ModelDb.Potion<PortionedMedicinalCuisinePotion>().Id;
        foreach (var player in room.CombatState.Players)
        {
            if (player.Potions.Count(potion => potion.Id == potionId) >= HealthConscious.RequiredPotions)
            {
                HealthConscious.Unlock(player);
            }
        }

        return Task.CompletedTask;
    }
}
