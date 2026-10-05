using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.potions;

/// <summary>
/// 超ZUN啤酒：从三张东方先古之民卡牌中选择一张加入手牌，这张牌在这回合免费打出。
/// </summary>
[Pool(typeof(SharedPotionPool))]
public sealed class SuperZunBeerPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;
    public override string? CustomPackedImagePath => $"res://images/potion/{GetType().Name}.png";
    public override string? CustomPackedOutlinePath => $"res://images/potion/{GetType().Name}_outline.png";

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        // 获取所有继承 TouhouAncientCards 的卡牌，筛选可战斗生成的
        var ancientCards = ModelDb.AllCards
            .Where(c => c is TouhouAncientCards && c.CanBeGeneratedInCombat)
            .Where(x=>x.Type is CardType.Attack or CardType.Skill or CardType.Power )
            .ToList();

        if (ancientCards.Count == 0) return;

        // 不用 CardFactory.GetDistinctForCombat：其 FilterForCombat 会剔除 Ancient 稀有度卡
        var rng = base.Owner.RunState.Rng.CombatCardGeneration;
        var options = ancientCards.UnstableShuffle(rng).Take(3)
            .Select(c => base.Owner.Creature.CombatState.CreateCard(c, base.Owner))
            .ToList();

        var card = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, base.Owner, canSkip: true);
        if (card == null) return;

        card.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, creator: base.Owner);
    }
}