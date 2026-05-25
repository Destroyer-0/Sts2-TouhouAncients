using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 蛊毒：回合结束时，如果这张牌在手中，其费用降低1直至打出，本局游戏中给予的中毒层数+5。
/// </summary>
public class MedicinePoison : CustomEnchantmentModel
{
    
 //   public override bool HasExtraCardText => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("PoisonBonus", 3)
    ];


    protected override void OnEnchant()
    {
        Card.AddKeyword(CardKeyword.Eternal);
    }

    /// <summary>
    /// 回合结束时，如果这张牌在手中，累计费用减免+1。
    /// </summary>
    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (!HasCard) return;
        if (side != base.Card.Owner.Creature.Side) return;
        if (base.Card.Pile?.Type != PileType.Hand) return;
        Card.EnergyCost.AddUntilPlayed(-1);
        if (Card.DynamicVars.ContainsKey("PoisonPower"))
        {
            Card.DynamicVars["PoisonPower"].BaseValue += DynamicVars["PoisonBonus"].IntValue;
        }
    }

    //
    // /// <summary>
    // /// 打出时：重置累计费用减免（"直至打出"），并给予额外中毒层数。
    // /// </summary>
    // public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    // {
    //     if (cardPlay == null) return;
    //     if (cardPlay.Card != base.Card) return;
    //
    //     // 额外给予中毒层数
    //     if (TouhouAncients_PoisonBonus > 0 && cardPlay.Target != null)
    //     {
    //         await PowerCmd.Apply<PoisonPower>(cardPlay.Target, TouhouAncients_PoisonBonus, base.Card.Owner.Creature, base.Card);
    //     }
    // }
}