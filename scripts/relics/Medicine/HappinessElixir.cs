using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.potions;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 幸福的秘药：
/// 获得一瓶卡米莉亚。第二幕的Boss战结束后额外掉落一瓶。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HappinessElixir : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPotion<CamelliaPotion>()];

    public override async Task AfterObtained()
    {
        var potion = ModelDb.Potion<CamelliaPotion>().ToMutable();
        await PotionCmd.TryToProcure(potion, base.Owner);
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != base.Owner) return false;
        if (room == null || room.RoomType != RoomType.Boss) return false;
        if (player.RunState.CurrentActIndex != 1) return false; // 第二幕

        Flash();
        rewards.Add(new PotionReward(ModelDb.Potion<CamelliaPotion>().ToMutable(), player));
        return true;
    }
}