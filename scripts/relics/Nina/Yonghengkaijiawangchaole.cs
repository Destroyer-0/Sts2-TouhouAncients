using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(EventRelicPool))]
public class Yonghengkaijiawangchaole : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PlatingPower>(7),
        new EnergyVar(1)
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<PlatingPower>(),
        HoverTipFactory.ForEnergy(this),
    ];

    private int _shouldAddEnergyAfterReset;
    private bool _canAddEnergy;

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
        {
            _canAddEnergy = false;
            Flash();
            await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(),base.Owner.Creature, base.DynamicVars["PlatingPower"].BaseValue, base.Owner.Creature, null);
        }
    }

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner) return;
        if (_canAddEnergy) return;
        if (_shouldAddEnergyAfterReset <= 0) return;
        _canAddEnergy = true;
        Flash();
        await PlayerCmd.GainEnergy(_shouldAddEnergyAfterReset, base.Owner);
        _shouldAddEnergyAfterReset = 0;
    }

 //    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
	// {
	// 	if (side == base.Owner.Creature.Side && combatState.RoundNumber <= 1)
	// 	{
 //            //await PowerCmd.Apply<PlatingPower>(base.Owner.Creature, base.DynamicVars["PlatingPower"].BaseValue, base.Owner.Creature, null);
	// 	}
	// }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is PlatingPower platingPower && power.Owner == base.Owner.Creature && amount > 0)
        {
            GD.PrintErr($"获得覆甲{amount}，当前层数={platingPower.Amount}");
            if (_canAddEnergy)
            {
                Flash();
                await PlayerCmd.GainEnergy(1m, base.Owner);
            }
            else
            {
                GD.PrintErr($"战斗还未开始，改为存储。");
                _shouldAddEnergyAfterReset++;
            }
        }
    }
}