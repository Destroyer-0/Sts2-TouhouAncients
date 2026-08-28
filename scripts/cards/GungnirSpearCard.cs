using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 神枪「冈格尼尔」：2费攻击。保留。
/// 回合结束时，将这张牌返回你的手牌。
/// 造成等同于目标格挡值2（升级后3）倍的伤害。
/// </summary>
[Pool(typeof(EventCardPool))]
public class GungnirSpearCard : TouhouAncientCards
{
    public override string? Author => "こぞう";
    
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    //public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new ExtraDamageVar(3m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? target) =>
            target?.Block ?? 0),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Block),
    ];

    public GungnirSpearCard() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (!CombatManager.Instance.IsInProgress || player != this.Owner || this.HasBeenRemovedFromState) return;
        if (this.Pile != null && this.Pile.Type != PileType.Hand)
        {
            await CardPileCmd.Add(this, PileType.Hand, clonedBy: this);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.CalculatedDamage).FromCard(this,cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    public override CardLocation ModifyCardPlayResultLocation(CardModel card,
        bool isAutoPlay, ResourceInfo resources, CardLocation cardLocation)
    {
        if (!CombatManager.Instance.IsInProgress || card != this || card.HasBeenRemovedFromState)
            return base.ModifyCardPlayResultLocation(card, isAutoPlay, resources, cardLocation);
        return new CardLocation(cardLocation.player, PileType.Hand, CardPilePosition.Top);
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (!CombatManager.Instance.IsInProgress || card != this || card.HasBeenRemovedFromState) return;
        if (card.Pile != null && card.Pile.Type != PileType.Hand && PileType.Hand.GetPile(Owner).Cards.Count < CardPile.MaxCardsInHand)
        {
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, clonedBy: this);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}