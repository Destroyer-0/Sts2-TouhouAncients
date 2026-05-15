using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 幸运白兔：0费技能。消耗（升级后去除）。
/// 抽一张牌，随机化你的卡牌耗能。本回合费用为0的卡牌造成的伤害与提供的格挡翻倍。
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
        new CardsVar(1)
    ];

    public LuckyRabbit() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;
        if (player?.PlayerCombatState == null) return;

        await CardPileCmd.Draw(choiceContext, 1, player);

        foreach (var card in player.PlayerCombatState.Hand.Cards)
        {
            if (card.EnergyCost.Canonical < 0) continue;
            var randomCost = player.RunState.Rng.CombatEnergyCosts.NextInt(4);
            card.EnergyCost.SetThisTurn(randomCost);
        }

        await PowerCmd.Apply<LuckyRabbitPower>(player.Creature, 2m, player.Creature, this);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}