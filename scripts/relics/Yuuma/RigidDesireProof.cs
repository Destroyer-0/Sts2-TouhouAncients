using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 刚欲之证：每回合获得1能量。你不能连续打出TypeLimit张同类型牌。
/// 通过 ShouldPlay 重写实现。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class RigidDesireProof : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";
    

    public override bool HasUponPickupEffect => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new DynamicVar("TypeLimit", 3),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(this),
    ];

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner) return amount;
        return amount + DynamicVars.Energy.IntValue;
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner != base.Owner) return true;
        if (autoPlayType != AutoPlayType.None) return true;

        int limit = DynamicVars["TypeLimit"].IntValue;
        var cardPlay = CombatManager.Instance.History.Entries.OfType<CardPlayStartedEntry>().Where(x => x.HappenedThisTurn(Owner.Creature.CombatState)).ToList();
        if (cardPlay.Count < limit - 1) return true;
        for (int i = cardPlay.Count - limit + 1; i < cardPlay.Count; i++)
        {
            if (cardPlay[i].CardPlay.Card.Type != card.Type) return true;
        }
        return false;
    }
}
