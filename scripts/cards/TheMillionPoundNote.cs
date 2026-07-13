using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 疫病支票 (The Million Pound Note)
/// 2(1)费，消耗，保留。消耗你所有的债务。
/// 进入手牌时给予玩家百万英镑 Power：每回合前 2 张牌免费打出。
/// 离开手牌且手牌中无其他疫病支票时移除该 Power。
/// </summary>
[Pool(typeof(EventCardPool))]
public class TheMillionPoundNote : TouhouAncientCards
{
    private const int energyCost = 2;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Debt>();

    public TheMillionPoundNote() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var debts = Owner.PlayerCombatState.AllCards.Where(c => c is Debt && c.Pile.Type != PileType.Exhaust);
        
        foreach (var item in debts)
        {
            await CardCmd.Exhaust(choiceContext, item);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != base.Owner.Creature.Side) return;
        if (base.Owner.PlayerCombatState == null) return;
        if (!participants.Contains(base.Owner.Creature))
        {
            return;
        }

        if (!PileType.Hand.GetPile(base.Owner).Cards.Contains(this)) return;

        await CardPileCmd.AddToCombatAndPreview<Debt>(base.Owner.Creature, PileType.Draw, 1, creator: base.Owner,CardPilePosition.Random);
    }
    /// <summary>
    /// 卡牌变更牌堆时：进入手牌则给予 Power，离开手牌则由 Power 自行检查移除。
    /// </summary>
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != this) return;
        if (card.Owner != Owner) return;
        if (Owner?.Creature?.CombatState == null) return;
        if (oldPileType == PileType.Hand && Pile?.Type == PileType.Hand) return;
        if (Pile?.Type == PileType.Hand)
        {
            await PowerCmd.Apply<TheMillionPoundNotePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 2m,
                Owner.Creature, this);
        }
        else if (oldPileType == PileType.Hand)
        {
            var millionPoundPower = Owner.Creature.GetPower<TheMillionPoundNotePower>();
            if (millionPoundPower != null)
            {
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), millionPoundPower, -2, Owner.Creature, this);
            }
        }
    }
}