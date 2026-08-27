using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Afflictions;

public sealed class Devoured : TouhouAncientAfflictionModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (card != Card)
        {
            return false;
        }
        
        return keywords.Add(CardKeyword.Exhaust);
    }
    
    // public override bool CanAfflictCardType(CardType cardType)
    // {
    //     if ((uint)(cardType - 1) <= 1u)
    //     {
    //         return true;
    //     }
    //
    //     return false;
    // }
}