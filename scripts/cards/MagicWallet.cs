using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

[Pool(typeof(EventCardPool))]
public class MagicWallet : TouhouAncientCards
{
    public override string? Author => "Feyoi";

    private const int energyCost = 0;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new EnergyVar(1),
    ];

    public MagicWallet() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player.Creature.CombatState == null) return;

        // 生成N张随机无色牌，设为免费并加入手牌
        var colorlessCards = CardFactory.GetDistinctForCombat(
            player,
            ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint),
            DynamicVars.Cards.IntValue,
            player.RunState.Rng.CombatCardGeneration);

        foreach (var card in colorlessCards)
        {
            card.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            card.EnergyCost.SetThisTurnOrUntilPlayed(0);
            //card.AddKeyword(CardKeyword.Exhaust);
        }

        // 自身费用+1，生成数量+1
        EnergyCost.AddThisCombat(DynamicVars.Energy.IntValue);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
