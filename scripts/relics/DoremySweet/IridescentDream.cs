using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 虹光之梦：拾起时，将你的初始遗物升级成先古版本，并将「古老牙齿」所指代的初始卡牌从牌组中移除。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class IridescentDream : TouhouAncientRelics
{
    /// <summary>
    /// 「古老牙齿」所对应的初始卡牌。与原版 <see cref="ArchaicTooth"/> 的 TranscendenceUpgrades 键集合一致
    /// （该字典是私有的，故在此显式列出，以便用同一条规则选取要移除的牌）。
    /// </summary>
    private static ModelId[] StarterCardIds =>
    [
        ModelDb.Card<Bash>().Id,
        ModelDb.Card<Neutralize>().Id,
        ModelDb.Card<Unleash>().Id,
        ModelDb.Card<FallingStar>().Id,
        ModelDb.Card<Dualcast>().Id
    ];

    private List<IHoverTip> _extraHoverTips = new();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("StarterRelic"),
        new StringVar("UpgradedRelic"),
        new StringVar("StarterCard")
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => _extraHoverTips;

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _extraHoverTips = new List<IHoverTip>();
    }

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        Flash();

        // 1) 初始遗物 → 先古版本（复用原版「欧洛巴斯之触」的升级对照表）
        var starterRelic = player.Relics.FirstOrDefault(r => r.Rarity == RelicRarity.Starter);
        if (starterRelic != null)
        {
            var upgraded = ModelDb.Relic<TouchOfOrobas>().GetUpgradedStarterRelic(starterRelic);
            ((StringVar)base.DynamicVars["StarterRelic"]).StringValue = starterRelic.Title.GetFormattedText();
            ((StringVar)base.DynamicVars["UpgradedRelic"]).StringValue = upgraded.Title.GetFormattedText();
            _extraHoverTips.AddRange(starterRelic.HoverTips);
            _extraHoverTips.AddRange(HoverTipFactory.FromRelic(upgraded));
            await RelicCmd.Replace(starterRelic, upgraded.ToMutable());
        }

        // 2) 「古老牙齿」所指代的初始卡牌 → 从牌组中移除
        var starterCard = player.Deck.Cards.FirstOrDefault(c => StarterCardIds.Contains(c.Id));
        if (starterCard != null)
        {
            ((StringVar)base.DynamicVars["StarterCard"]).StringValue = starterCard.Title;
            _extraHoverTips.Add(HoverTipFactory.FromCard(starterCard));
            await CardPileCmd.RemoveFromDeck(starterCard);
        }
    }
}
