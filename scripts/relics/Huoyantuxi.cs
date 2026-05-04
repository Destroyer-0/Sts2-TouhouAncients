using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

public class Huoyantuxi : TouhouAncientRelics
{
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FlameBarrierPower>(8)];
    
    // public override async Task AfterRoomEntered(AbstractRoom room)
    // {
    //     if (room is CombatRoom)
    //     {
    //         Flash();
    //         await PowerCmd.Apply<ThornsPower>(base.Owner.Creature, base.DynamicVars["FlameBarrierPower"].BaseValue, base.Owner.Creature, null);
    //     }
    // }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        Flash();
        await PowerCmd.Apply<FlameBarrierPower>(base.Owner.Creature, base.DynamicVars["FlameBarrierPower"].BaseValue,
            base.Owner.Creature, null);
    }
}