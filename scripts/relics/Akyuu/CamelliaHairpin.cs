using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using TouhouAncients.Scripts.Rewards;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 山茶花发饰：每当你在休息处休息时，你可以变化一张牌。
/// 走休息奖励面板（原版小邮箱同款），奖励自身可取消即「可以不变化」。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class CamelliaHairpin : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Transform)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override bool TryModifyRestSiteHealRewards(Player player, List<Reward> rewards, bool isMimicked)
    {
        if (player != base.Owner) return false;
        if (isMimicked) return false;                       // 事件模仿的「休息」不算
        if (!PileType.Deck.GetPile(player).Cards.Any()) return false;

        rewards.Add(new TransformCardReward(player) { Amount = 1 });
        return true;
    }
}
