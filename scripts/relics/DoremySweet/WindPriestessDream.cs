using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 风祝之梦：你遇到的前 3 次卡牌奖励中，每张可附魔的牌随机获得 锋利2 / 灵巧2 / 迅捷1 之一。
/// 计数与「前 N 次」写法照原版遗物 SilverCrucible。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class WindPriestessDream : TouhouAncientRelics
{
    private const int MaxRewards = 3;
    private const decimal SharpAmount = 2m;
    private const decimal NimbleAmount = 2m;
    private const decimal SwiftAmount = 1m;

    private int _timesUsed;

    public override bool IsUsedUp => TimesUsed >= MaxRewards;
    public override bool ShowCounter => !IsUsedUp;
    public override int DisplayAmount => MaxRewards - TimesUsed;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(MaxRewards)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<Sharp>((int)SharpAmount)
            .Concat(HoverTipFactory.FromEnchantment<Nimble>((int)NimbleAmount))
            .Concat(HoverTipFactory.FromEnchantment<Swift>((int)SwiftAmount));

    [SavedProperty]
    public int TimesUsed
    {
        get => _timesUsed;
        set
        {
            AssertMutable();
            _timesUsed = value;
            InvokeDisplayAmountChanged();
            CheckIfUsedUp();
        }
    }

    public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewards, CardCreationOptions options)
    {
        if (player != base.Owner) return false;
        if (IsUsedUp) return false;
        if (!options.Flags.HasFlag(CardCreationFlags.IsCardReward)) return false;

        foreach (var result in cardRewards)
        {
            var enchanted = EnchantRandom(base.Owner.RunState.CloneCard(result.Card));
            if (enchanted == null) continue;
            result.ModifyCard(enchanted, this);
        }

        return true;
    }

    public override Task AfterModifyingCardRewardOptions()
    {
        if (IsUsedUp) return Task.CompletedTask;
        TimesUsed++;
        return Task.CompletedTask;
    }

    private void CheckIfUsedUp()
    {
        if (IsUsedUp) base.Status = RelicStatus.Disabled;
    }

    private enum Kind
    {
        Sharp,
        Nimble,
        Swift,
    }

    /// <summary>从该牌实际可接受的附魔里随机取一种施加；一张都套不上时返回 null。</summary>
    private CardModel? EnchantRandom(CardModel card)
    {
        var candidates = new List<Kind>(3);
        if (ModelDb.Enchantment<Sharp>().CanEnchant(card)) candidates.Add(Kind.Sharp);
        if (ModelDb.Enchantment<Nimble>().CanEnchant(card)) candidates.Add(Kind.Nimble);
        if (ModelDb.Enchantment<Swift>().CanEnchant(card)) candidates.Add(Kind.Swift);
        if (candidates.Count == 0) return null;

        switch (candidates[base.Owner.RunState.Rng.Niche.NextInt(candidates.Count)])
        {
            case Kind.Sharp:
                CardCmd.Enchant<Sharp>(card, SharpAmount);
                break;
            case Kind.Nimble:
                CardCmd.Enchant<Nimble>(card, NimbleAmount);
                break;
            default:
                CardCmd.Enchant<Swift>(card, SwiftAmount);
                break;
        }

        return card;
    }
}
