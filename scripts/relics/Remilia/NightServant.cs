using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
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
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<ServantSakuya>(),
        HoverTipFactory.FromCard<ServantPatchouli>(),
        HoverTipFactory.FromCard<ServantHongmeiling>(),
    ];

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> options,
        CardCreationOptions creationOptions)
    {
        if (base.Owner != player) return false;

        if (creationOptions.Source != CardCreationSource.Encounter)
        {
            return false;
        }
        Flash();
        // 创建 3 个侍从的实例（不能用 ModelDb.Card<>，那是规范模型，无法加入牌组）
        var servantCards = new List<CardModel>
        {
            player.RunState.CreateCard<ServantSakuya>(player),
            player.RunState.CreateCard<ServantPatchouli>(player),
            player.RunState.CreateCard<ServantHongmeiling>(player)
        };

        // 随机选一张
        var rng = base.Owner.RunState.Rng.Shuffle;
        var chosenCard = servantCards[rng.NextInt(servantCards.Count)];
        var result = new CardCreationResult(chosenCard);
        result.ModifyCard(chosenCard, this);
        options.Add(result);
        return true;
    }
}