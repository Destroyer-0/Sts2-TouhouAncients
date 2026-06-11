using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Enchantments;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts.cards;

[Pool(typeof(EventCardPool))]
public class TheKoishiEye : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Instinct>()
        .Append(HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientKoishiUnplayable));

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Ethereal];

    public TheKoishiEye() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;
        var rng = player.RunState.Rng;

        // 1. 收集抽牌堆、手牌、弃牌堆中的卡牌
        var allCards = new List<CardModel>();
        allCards.AddRange(PileType.Draw.GetPile(player).Cards);
        allCards.AddRange(PileType.Hand.GetPile(player).Cards);
        allCards.AddRange(PileType.Discard.GetPile(player).Cards);

        // 排除本卡自身和不可打出的卡牌
        allCards = allCards.Where(c => c != cardPlay.Card).ToList();
        if (allCards.Count == 0) return;

        // 2. 随机打乱，取 N 张
        var toPlay = allCards.StableShuffle(rng.Shuffle).Take(base.DynamicVars.Cards.IntValue).ToList();

        // 3. 逐一自动打出
        foreach (var card in toPlay)
        {
            if (CombatManager.Instance.IsOverOrEnding) break;
            await CardCmd.AutoPlay(choiceContext, card, null);
        }

        // 4. 过滤出仍在游戏中的卡牌（未被消耗/移出游戏）
        var inGameCards = toPlay.Where(c => c is { IsInCombat: true, HasBeenRemovedFromState: false }).ToList();
        if (inGameCards.Count == 0) return;

        // 5. 为随机一张攻击牌附魔本能
        var instinct = ModelDb.Enchantment<Instinct>().ToMutable();
        var attackCards = inGameCards.Where(c => c.Type == CardType.Attack && instinct.CanEnchant(c)).ToList();
        if (attackCards.Count > 0)
        {
            var target = attackCards[rng.Shuffle.NextInt(attackCards.Count)];
            CardCmd.Enchant(instinct, target, 1m);
        }

        // 6. 为随机一张牌添加不能被打出
        var unplayableTarget = inGameCards[rng.Shuffle.NextInt(inGameCards.Count)];
        unplayableTarget.AddKeyword(TouhouAncientKeywords.TouhouAncientKoishiUnplayable);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
    }
}