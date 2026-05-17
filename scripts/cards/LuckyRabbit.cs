using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 幸运白兔：0费技能。消耗（升级后去除）。
/// 抽牌，随机化手牌耗能。为本场战斗中手牌里费用为0的卡牌永久添加重放。
/// </summary>
[Pool(typeof(EventCardPool))]
public class LuckyRabbit : TouhouAncientCards
{
    private const int energyCost = 0;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.ReplayStatic)];
    

    public LuckyRabbit() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;
        if (player?.PlayerCombatState == null) return;

        // 抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, player);

        // 随机化所有手牌的耗能
        foreach (var card in player.PlayerCombatState.Hand.Cards)
        {
            if (card.EnergyCost.Canonical < 0) continue;
            var randomCost = player.RunState.Rng.CombatEnergyCosts.NextInt(4);
            card.EnergyCost.SetThisTurn(randomCost);
        }

        // 为本场战斗中手牌里费用为0的卡牌永久添加重放
        foreach (var card in player.PlayerCombatState.Hand.Cards)
        {
            if (card.EnergyCost.Canonical < 0) continue;
            if (card.EnergyCost.GetResolved() != 0) continue;
            card.BaseReplayCount++;
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}