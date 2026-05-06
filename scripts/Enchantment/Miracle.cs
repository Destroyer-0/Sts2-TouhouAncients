using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.Enchantment;

public class Miracle : CustomEnchantmentModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(Card),
    ];

    public override bool CanEnchantCardType(CardType cardType)
    {
        // Only Attack, Skill, Power
        if ((uint)(cardType - 1) <= 2u)
        {
            return true;
        }

        return false;
    }

    protected override void OnEnchant()
    {
        base.Card.AddKeyword(CardKeyword.Exhaust);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        var player = base.Card.Owner;
        if (player.Creature.CombatState == null) return;

        await PlayerCmd.GainEnergy(1m, player);

        //选择一张随机牌
        var allCards = ModelDb.AllCharacterCardPools.SelectMany(x=>x.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint));
        var colorlessCards = ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
        
        CardModel? cardModel = CardFactory.GetDistinctForCombat(
            player,
            allCards.Concat(colorlessCards),
            1,
            player.RunState.Rng.CombatCardGeneration
        ).FirstOrDefault();
        
        if (cardModel != null)
        {
            if (base.Card.IsUpgraded)
            {
                CardCmd.Upgrade(cardModel);
            }
            
            CardCmd.Enchant<Miracle>(cardModel, 1m);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(cardModel, PileType.Draw, addedByPlayer: true, CardPilePosition.Random));
        }
    }
}