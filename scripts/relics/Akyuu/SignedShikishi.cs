using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 签名色纸：每次卡牌奖励中，都会有随机一张牌被附魔：亲笔。
/// 若你的牌组中有带有亲笔附魔的牌，则你可以在休息处签售（移除一张亲笔牌换 100 金币，不占用原有休息处选项）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SignedShikishi : TouhouAncientRelics
{
    /// <summary>附魔层数（亲笔无叠层需求，固定 1）。</summary>
    private const decimal EnchantAmount = 1m;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. HoverTipFactory.FromEnchantment<Autograph>(),
        new HoverTip(
            new LocString("rest_site_ui", "OPTION_SIGNED_SHIKISHI.name"),
            new LocString("rest_site_ui", "OPTION_SIGNED_SHIKISHI.description"))
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("EnchantmentName", ModelDb.Enchantment<Autograph>().Title.GetFormattedText())
    ];

    /// <summary>
    /// 一组卡牌奖励里随机一张可附魔的牌盖上亲笔。照原版迅捷护符 <c>WingCharm</c>：
    /// 用共享的 <c>RunState.Rng.Niche</c>（各端同一结果，不能用 PlayerRng）。
    /// </summary>
    public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewards,
        CardCreationOptions options)
    {
        if (player != base.Owner) return false;

        Autograph canonicalAutograph = ModelDb.Enchantment<Autograph>();
        List<CardCreationResult> candidates = cardRewards.Where(r => canonicalAutograph.CanEnchant(r.Card)).ToList();
        if (candidates.Count == 0) return false;

        var target = base.Owner.RunState.Rng.Niche.NextItem(candidates);
        if (target == null) return false;

        CardModel card = base.Owner.RunState.CloneCard(target.Card);
        CardCmd.Enchant<Autograph>(card, EnchantAmount);
        target.ModifyCard(card, this);
        return true;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != base.Owner) return false;
        if (!HasSignable(player)) return false;

        options.Add(new SignedShikishiRestSiteOption(player, this));
        return true;
    }

    /// <summary>
    /// 签售不占用休息处选项：选中后其余选项保留（照原版微型帐篷 <c>MiniatureTent</c> 的写法，
    /// 但只在「刚选的正是签售」时放行，见 <see cref="TouhouAncientRelics.LastRestSiteChoice"/>）。
    /// </summary>
    public override bool ShouldDisableRemainingRestSiteOptions(Player player)
    {
        if (player != base.Owner) return true;
        if (LastRestSiteChoice(player) != SignedShikishiRestSiteOption.OptionIdValue) return true;

        Flash();
        return false;
    }

    /// <summary>牌组里有没有带亲笔附魔的牌。</summary>
    public bool HasSignable(Player player) =>
        PileType.Deck.GetPile(player).Cards.Any(c => c.Enchantment is Autograph);

    /// <summary>签售：移除一张亲笔牌（金币由附魔自身的 BeforeCardRemoved 给）。返回是否真的移除了。</summary>
    public async Task<bool> Sign()
    {
        var player = base.Owner;
        if (player?.Creature == null) return false;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1) { Cancelable = true };
        var target = (await CardSelectCmd.FromDeckGeneric(player, prefs, c => c.Enchantment is Autograph))
            .FirstOrDefault();
        if (target == null) return false;

        Flash();
        await CardPileCmd.RemoveFromDeck(target);
        return true;
    }
}

/// <summary>签售休息处选项。选中后不消耗其他休息处选项（见 relic.ShouldDisableRemainingRestSiteOptions）。</summary>
public class SignedShikishiRestSiteOption(Player owner, SignedShikishi relic) : RestSiteOption(owner)
{
    public const string OptionIdValue = "SIGNED_SHIKISHI";

    public override string OptionId => OptionIdValue;

    public override bool IsEnabled => relic.HasSignable(Owner);

    public override async Task<bool> OnSelect()
    {
        if (!IsEnabled) return false;

        // 取消选牌返回 false：选项留在原地、UI 重新启用，不消耗这次休息
        return await relic.Sign();
    }
}
