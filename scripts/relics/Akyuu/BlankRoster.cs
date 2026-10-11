using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 空白名册：拾起时，随机获得2件稗田阿求遗物，将一张积劳成疾加入你的牌组。
/// 候选来自阿求的选项池（照原版涅奥骨骰），排除自身、已拥有与条件不满足的遗物。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BlankRoster : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<OverworkIllness>();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Count", 2m)];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        Flash();

        var owned = player.Relics.Select(r => r.Id).ToHashSet();
        var candidates = ModelDb.Event<HiedaNoAkyuuAncient>().AllPossibleOptions
            .Select(o => o.Relic)
            .OfType<RelicModel>()
            .Where(r => r is not BlankRoster)
            .Where(r => !owned.Contains(r.Id))
            .Where(r => r is not TouhouAncientRelics relic || relic.CanAppear(player))
            .ToList();
        player.PlayerRng.Rewards.Shuffle(candidates);

        var rewards = candidates.Take(base.DynamicVars["Count"].IntValue)
            .Select(r => (Reward)new RelicReward(r, player))
            .ToList();
        if (rewards.Count > 0)
        {
            await new RewardsSet(player).WithCustomRewards(rewards).WithSkippingDisallowed().Offer();
        }

        var curse = player.RunState.CreateCard<OverworkIllness>(player);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(curse, PileType.Deck), 2f);
    }
}
