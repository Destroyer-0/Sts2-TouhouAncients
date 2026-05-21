using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 童忆书包：
/// 用污浊药水（FOUL_POTION）填满你的空药水栏位。
/// 将战斗结束时的金币奖励替换为1瓶污浊药水（不再掉落金币奖励，并掉落1瓶污浊药水）。
/// 自身免疫污浊药水造成的伤害。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class ChildhoodBag : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("PotionCount", 1)
    ];


    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        // 用幸运补剂填满所有空药水栏位
        for (int i = 0; i < base.Owner.MaxPotionCount; i++)
        {
            if (base.Owner.GetPotionAtSlotIndex(i) == null)
            {
                await PotionCmd.TryToProcure<FoulPotion>(base.Owner);
            }
        }
    }

    /// <summary>
    /// 战斗结束时，将金币奖励替换为1瓶污浊药水。
    /// </summary>
    public override bool TryModifyRewards(Player player, System.Collections.Generic.List<Reward> rewards, AbstractRoom? room)
    {
        if (player != base.Owner) return false;
        if (room?.RoomType != RoomType.Monster && room?.RoomType != RoomType.Elite && room?.RoomType != RoomType.Boss)
            return false;

        var modified = false;
        for (var i = rewards.Count - 1; i >= 0; i--)
        {
            if (rewards[i] is GoldReward)
            {
                // 替换金币奖励为污浊药水
                rewards[i] = new PotionReward(ModelDb.Potion<FoulPotion>(),player);
                modified = true;
                break;
            }
        }

        // 如果没有金币奖励，也额外添加一瓶污浊药水
        if (!modified)
        {
            rewards.Add(new PotionReward(ModelDb.Potion<FoulPotion>(), player));
            modified = true;
        }

        return modified;
    }

    /// <summary>
    /// 免疫污浊药水造成的伤害。
    /// TODO: 当前 STS2 API 暂无明确标记药水伤害来源的机制，需要后续补充精确检测。
    /// </summary>
    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner.Creature) return;
        // TODO: 补充污浊药水伤害检测逻辑
    }
}
