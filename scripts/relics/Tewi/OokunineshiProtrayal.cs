using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 大国主画像：将会有白兔们为你带来额外的战斗奖励
/// 战斗奖励将额外包含以下一项
/// 20~40金币
/// 卡牌奖励
/// 删一
/// 药水
/// 遗物
/// 蜡质遗物
/// </summary>
[Pool(typeof(EventRelicPool))]
public class OokunineshiProtrayal : TouhouAncientRelics
{
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != base.Owner)
        {
            return false;
        }
        if (room == null)
        {
            return false;
        }
        if (!room.RoomType.IsCombatRoom())
        {
            return false;
        }

        if (room.RoomType == RoomType.Boss && player.RunState.CurrentActIndex >= player.RunState.Acts.Count - 1)
        {
            return false;
        }

        //rewards.Add(new GoldReward(base.DynamicVars.Gold.IntValue, player));
        if (room is CombatRoom combatRoom)
        {
            var num = Owner.PlayerRng.Rewards.NextInt(10);
            switch (num)
            {
                case 0:
                case 1:
                    rewards.Add(new GoldReward(combatRoom.Encounter.MinGoldReward, combatRoom.Encounter.MaxGoldReward * 2, base.Owner));
                    break;
                case 2:
                case 3:
                    rewards.Add(new CardReward(CardCreationOptions.ForRoom(base.Owner, combatRoom.RoomType), 3, base.Owner));
                    break;
                case 4:
                    rewards.Add(new CardRemovalReward(base.Owner));
                    break;
                case 5:
                    rewards.Add(new PotionReward(base.Owner));
                    break;
                case 6:
                    rewards.Add(new RelicReward(RelicRarity.Common, base.Owner));
                    break;
                case 7:
                    rewards.Add(new PotionReward(ModelDb.Potion<BloodPotion>().ToMutable(), base.Owner));
                    break;
                default:
                    return false;
            }
        }
        Flash();
        return true;
    }
    
    //
    // public override Task AfterCombatVictory(CombatRoom room)
    // {
    //     AbstractRoom currentRoom = base.Owner.Creature.CombatState!.RunState.CurrentRoom;
    //     if (currentRoom is CombatRoom combatRoom)
    //     { 
    //         Flash();
    //
    //     }
    //
    //     return Task.CompletedTask;
    // }
}