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
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 御阿礼·砚：拾起时，从3张能力牌中选择1张加入你的牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MiareInkstone : TouhouAncientRelics
{
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
        if (!options.GetPossibleCards(player).Any()) return;

        var cards = CardFactory.CreateForReward(player, 3, options).Select(r => r.Card).ToList();
        if (cards.Count == 0) return;

        Flash();
        var picked = await CardSelectCmd.FromChooseACardScreen(
            new BlockingPlayerChoiceContext(), cards, player, canSkip: true);
        if (picked == null) return;

        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(picked, PileType.Deck), 2f);
    }
}
