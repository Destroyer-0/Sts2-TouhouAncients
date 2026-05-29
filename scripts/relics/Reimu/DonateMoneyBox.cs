using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 赛钱箱：当你进入休息处时，获得一组卡牌奖励与金币。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class DonateMoneyBox : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardRewards", 1m),
        new GoldVar(100),
        new DynamicVar("GoldMax", 150m)
    ];

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not RestSiteRoom) return;

        Flash();

        var rewards = new List<Reward>
        {
            new CardReward(CardCreationOptions.ForRoom(base.Owner, RoomType.Monster), 3, base.Owner),
            new GoldReward(base.DynamicVars.Gold.IntValue, base.DynamicVars["GoldMax"].IntValue, base.Owner)
        };

        await RewardsCmd.OfferCustom(base.Owner, rewards);
    }
}
