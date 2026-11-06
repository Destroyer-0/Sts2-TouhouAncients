using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 神秘果：每场战斗的前 3 回合额外获得 1 能量并抽 1 张牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MysteryFruit : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Turns", 3m),
        new CardsVar(1),
        new EnergyVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];

    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player == base.Owner && IsWithinFirstTurns(player)
            ? count + base.DynamicVars.Cards.IntValue
            : count;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != base.Owner) return;
        if (combatState != player.Creature.CombatState) return;
        if (!IsWithinFirstTurns(player))
        {
            Status = RelicStatus.Normal;
            InvokeDisplayAmountChanged();
            return;
        }

        Flash();
        Status = RelicStatus.Active;
        InvokeDisplayAmountChanged();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
    }

    private bool IsWithinFirstTurns(Player player) =>
        player.PlayerCombatState != null &&
        player.PlayerCombatState.TurnNumber <= base.DynamicVars["Turns"].IntValue;
}
