using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 迷你八卦炉：回合结束时，消耗1~3张手牌。
/// 若消耗的牌不小于2张，下个回合开始时获得1能量。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MiniHakkero : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1),new EnergyVar(1)];

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != base.Owner.Creature.Side) return;
        if (base.Owner.PlayerCombatState == null) return;

        var hand = PileType.Hand.GetPile(base.Owner);
        if (hand.Cards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 3);
        var selected = (await CardSelectCmd.FromHand(choiceContext, base.Owner, prefs, null, this)).ToList();

        if (selected.Count == 0) return;

        foreach (var card in selected)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        if (selected.Count >= 2)
        {
            Flash();
            await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, base.Owner.Creature,
                base.DynamicVars.Energy.BaseValue, base.Owner.Creature, null);
        }
    }
}
