using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 贪欲：获得永恒与重放1。
/// 如果你手中有带此附魔的牌，你必须先打出带有此附魔的牌。
/// </summary>
public class Greed : TouhouAncientEnchantmentModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Times", 1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Eternal),HoverTipFactory.Static(StaticHoverTip.ReplayDynamic,DynamicVars["Times"])];

    //public override bool HasExtraCardText => true;
    public override bool ShouldGlowRed => true;

    protected override void OnEnchant()
    {
        base.Card.AddKeyword(CardKeyword.Eternal);
    }


    public override int EnchantPlayCount(int originalPlayCount)
    {
        return originalPlayCount + 1;
    }
    
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (!HasCard) return true;
        if (card.Owner != base.Card.Owner)
        {
            return true;
        }
        CardPile? pile = base.Card.Pile;
        if (pile == null || pile.Type != PileType.Hand)
        {
            return true;
        }
        if (card.Enchantment is Greed)
        {
            return true;
        }
        if (autoPlayType != AutoPlayType.None)
        {
            return true;
        }
        return false;
    }
}
