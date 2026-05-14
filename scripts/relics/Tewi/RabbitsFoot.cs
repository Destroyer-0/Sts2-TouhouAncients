using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 兔脚：你可以以40金币的价格出售卡牌奖励。
/// 参照 PaelsWing / Driftwood 实现。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class RabbitsFoot : TouhouAncientRelics
{
    private const string _sellKey = "SELL_CARD";

    public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
    {
        if (base.Owner != player) return false;

        alternatives.Add(new CardRewardAlternative(
            _sellKey,
            OnSellCard,
            PostAlternateCardRewardAction.DismissScreenAndRemoveReward));
        return true;
    }

    private async Task OnSellCard()
    {
        // 出售时获得 40 金币
        Flash();
        await PlayerCmd.GainGold(50, base.Owner);
    }
}
