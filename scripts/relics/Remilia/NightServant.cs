using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 极夜侍仆：你的卡牌奖励会额外包含随机一张仆从牌（时间侍从、魔法侍从、罡气侍从）。
/// 参照 LastingCandy 的 TryModifyCardRewardOptions 实现。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class NightServant : TouhouAncientRelics
{
    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> options, CardCreationOptions creationOptions)
    {
        if (base.Owner != player) return false;

        // 构建 3 个侍从的候选池
        var servantCards = new List<CardModel>
        {
            ModelDb.Card<ServantSakuya>(),
            ModelDb.Card<ServantPatchouli>(),
            ModelDb.Card<ServantHongmeiling>()
        };

        // 用均匀稀有度创建临时选项，避免单稀有度断言
        var servantOptions = new CardCreationOptions(servantCards, CardCreationSource.Other, CardRarityOddsType.Uniform)
            .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications);

        var created = CardFactory.CreateForReward(base.Owner, 1, servantOptions).FirstOrDefault();
        if (created?.Card == null) return false;

        var result = new CardCreationResult(created.Card);
        result.ModifyCard(created.Card, this);
        options.Add(result);
        return true;
    }
}
