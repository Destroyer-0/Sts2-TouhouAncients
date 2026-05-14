using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 兔角契约：拾起时获得200金币并将一张债务加入牌组。
/// 每场战斗结束时获得当前金币20%的金币。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class RabbitHornContract : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<Debt>()
    ];
    public override async Task AfterObtained()
    {
        Flash();
        await PlayerCmd.GainGold(200, base.Owner);
        await CardPileCmd.AddCurseToDeck<Debt>(base.Owner);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        var gold = base.Owner.Gold;
        var bonus = gold / 5;
        if (bonus > 0)
        {
            Flash();
            room.AddExtraReward(base.Owner, new GoldReward(bonus, base.Owner));
        }

        return Task.CompletedTask;
    }
}
