using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Afflictions;

/// <summary>
/// 抵押物：无法被打出并获得保留，在手中累计停留2回合后移除此侵蚀。
/// </summary>
public sealed class Collateral : TouhouAncientAfflictionModel
{
    public override bool HasExtraCardText => true;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HasCard
            ? [HoverTipFactory.FromKeyword(CardKeyword.Unplayable), HoverTipFactory.FromKeyword(CardKeyword.Retain)]
            : [];
    
    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (card != Card)
        {
            return false;
        }

        return keywords.Add(CardKeyword.Retain) && keywords.Add(CardKeyword.Unplayable);
    }
    
    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Card.Owner.Creature))
        {
            return Task.CompletedTask;
        }

        Amount--;
        if (Amount <= 0)
        {
            Card.ClearAfflictionInternal();
        }

        return Task.CompletedTask;
    }
}
