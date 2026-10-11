using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 积劳成疾：固有。保留。不能被打出。抽到时，给予自身1虚弱。
/// </summary>
[Pool(typeof(EventCardPool))]
public class OverworkIllness : TouhouAncientCards
{
    public override string? Author => "orga";

    public override bool UseAncientFrame => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Innate, CardKeyword.Retain, CardKeyword.Unplayable];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Weak", 1m)];

    public OverworkIllness() : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None, true)
    {
    }

    /// <summary>抽到自己时给持有者上虚弱。</summary>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        if (Owner?.Creature?.CombatState == null) return;

        await PowerCmd.Apply<WeakPower>(choiceContext, Owner.Creature, DynamicVars["Weak"].BaseValue, null, null);
    }
}
