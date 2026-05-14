using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 全人类的绯想天：3费能力。保留。
/// 打出并消耗所有当前手牌与消耗堆里的牌。
/// 升级后：升级、打出并消耗所有当前手牌与消耗堆里的牌。
/// </summary>
[Pool(typeof(EventCardPool))]
public class AllCreationHisou : TouhouAncientCards
{
    private const int energyCost = 3;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    public AllCreationHisou() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;
        if (player?.PlayerCombatState == null) return;

        // 快照：记录当前手牌（排除自身）和消耗堆的所有牌
        var handCards = player.PlayerCombatState.Hand.Cards
            .Where(c => c != cardPlay.Card)
            .ToList();

        var exhaustCards = player.PlayerCombatState.ExhaustPile.Cards.ToList();

        var allTargetCards = handCards.Concat(exhaustCards).ToList();
        bool shouldUpgrade = cardPlay.Card.IsUpgraded;

        foreach (var card in allTargetCards)
        {
            // 升级后：先升级每张牌
            if (shouldUpgrade && card.IsUpgradable)
            {
                CardCmd.Upgrade(card);
            }

            // 自动打出
            await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), card, target: null);

            // 打出后若牌还存在且不在消耗堆，则消耗它
            if (card.Pile != null && card.Pile.Type != PileType.None && !card.HasBeenRemovedFromState)
            {
                await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), card);
            }
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果在 OnPlay 中通过 cardPlay.Card.IsUpgraded 判断
    }
}
