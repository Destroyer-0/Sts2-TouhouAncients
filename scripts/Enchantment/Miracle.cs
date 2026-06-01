using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace TouhouAncients.Scripts.Enchantment;

public class Miracle : TouhouAncientEnchantmentModel
{
    public override bool HasExtraCardText => true;

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

        // 根据角色过滤卡牌：
        // - 非储君(Regent)：排除带有辉星(star cost)的牌
        // - 非亡灵契约师(Necrobinder)：排除带有 OstyAttack 标签的牌
        var isRegent = player.Relics.Any(x => x is DivineRight or DivineDestiny);
        var isNecrobinder = player.Relics.Any(x => x is BoundPhylactery or PhylacteryUnbound);

        bool IsAllowed(CardModel c) =>
            CanEnchant(c)
            && (isRegent || c is { CanonicalStarCost: < 0, HasStarCostX: false })
            && (isNecrobinder || !c.Tags.Contains(CardTag.OstyAttack));

        //选择一张随机牌
        var allCards = ModelDb.AllCharacterCardPools
            .SelectMany(x => x.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Where(IsAllowed);
        var colorlessCards = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(IsAllowed);

        var selected = allCards.Concat(colorlessCards).ToList();

        int tryTime = 3;
        while (tryTime>0)
        {
            CardModel? cardModel = CardFactory.GetDistinctForCombat(
                player,
                selected,
                1,
                player.RunState.Rng.CombatCardGeneration
            ).FirstOrDefault();
            if (cardModel != null && CanEnchant(cardModel))
            {
                if (base.Card.IsUpgraded)
                {
                    CardCmd.Upgrade(cardModel);
                }

                CardCmd.Enchant<Miracle>(cardModel, 1m);
                CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(cardModel, PileType.Draw, addedByPlayer: true, CardPilePosition.Random));
                break;
            }

            tryTime--;
        }
    }
}