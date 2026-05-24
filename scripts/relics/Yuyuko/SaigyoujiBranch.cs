using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 西行妖枯枝：每当你消耗一张牌，增加一张带有虚无的随机卡牌到你的手牌并获得1格挡。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class SaigyoujiBranch : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BlockAmount", 1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
    ];

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal)
    {
        if (card.Owner != base.Owner) return;

        Flash();

        List<CardModel> cards = CardFactory.GetForCombat(base.Owner, base.Owner.Character.CardPool.GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint), 1, base.Owner.RunState.Rng.CombatCardGeneration).ToList();

        foreach (var cardResult in await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true))
        {
            cardResult.cardAdded.AddKeyword(CardKeyword.Ethereal);
        }
        
        // 获得1格挡
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars["BlockAmount"].BaseValue, ValueProp.Unpowered,
            null);
    }
}