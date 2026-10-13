using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 御阿礼·砚：拾起时，从3张能力牌中选择1张加入你的牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MiareInkstone : TouhouAncientRelics
{
    private const int CardCount = 3;

    // Uniform 是按张等概率，稀有 Power 张数一多就扎堆，这里按稀有度再乘权重压稀有
    private const int RareWeight = 1;
    private const int CommonUncommonWeight = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        // 只出能力牌：Uniform 不会掷稀有度，避免筛完没有对应稀有度而抛异常
        var options = new CardCreationOptions(
                [player.Character.CardPool],
                CardCreationSource.Other,
                CardRarityOddsType.Uniform,
                c => c.Type == CardType.Power)
            .WithFlags(CardCreationFlags.NoUpgradeRoll);
        // 自己抽样就绕开了 CardFactory，得手动补这个钩子
        options = Hook.ModifyCardRewardCreationOptions(player.RunState, player, options);

        // 排除 Basic/Ancient，与 CardFactory 的 Uniform 分支保持一致
        var remaining = options.GetPossibleCards(player)
            .Where(c => c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient)
            .ToList();
        if (remaining.Count == 0) return;

        var rng = player.PlayerRng.Rewards;
        var cards = new List<CardModel>();
        for (var i = 0; i < CardCount && remaining.Count > 0; i++)
        {
            var picked = PickWeighted(remaining, rng);
            remaining.Remove(picked);
            cards.Add(player.RunState.CreateCard(picked, player));
        }
        if (cards.Count == 0) return;

        Flash();
        var selected = await CardSelectCmd.FromChooseACardScreen(
            new BlockingPlayerChoiceContext(), cards, player, canSkip: true);
        if (selected == null) return;

        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(selected, PileType.Deck), 2f);
    }

    // 不放回加权抽样
    private CardModel PickWeighted(List<CardModel> pool, Rng rng)
    {
        var total = 0;
        foreach (var card in pool) total += WeightOf(card);

        var roll = rng.NextInt(total);
        foreach (var card in pool)
        {
            roll -= WeightOf(card);
            if (roll < 0) return card;
        }
        return pool[^1];
    }

    private int WeightOf(CardModel card)
        => card.Rarity == CardRarity.Rare ? RareWeight : CommonUncommonWeight;
}
