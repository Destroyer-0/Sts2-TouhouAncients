using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Hooks;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics.IdleKoishi;

/// <summary>
/// 无意识麦克风：进入嗨到爆时所有敌人失去 1 力量并在本回合额外失去 2 力量；
/// 本场战斗首次进入额外获得 1 无实体。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class UnconsciousMicrophone : TouhouAncientRelics, IHypedEnteredListener
{
    private const int StrengthLoss = 1;
    private const int ExtraStrengthLoss = 2;
    private const int Intangible = 1;

    private bool _enteredThisCombat;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("StrengthLoss", StrengthLoss),
        new DynamicVar("ExtraStrengthLoss", ExtraStrengthLoss),
        new DynamicVar("Intangible", Intangible),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<IntangiblePower>(),
    ];

    public override Task BeforeCombatStart()
    {
        _enteredThisCombat = false;
        return Task.CompletedTask;
    }

    public async Task AfterHypedEntered(PlayerChoiceContext choiceContext)
    {
        var player = base.Owner;
        if (player?.Creature?.CombatState is not { } combatState) return;

        var enemies = combatState.GetOpponentsOf(player.Creature).Where(enemy => enemy.IsAlive).ToList();
        if (enemies.Count > 0)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, enemies, -StrengthLoss, player.Creature, null);
            await PowerCmd.Apply<UnconsciousMicStrengthDownPower>(choiceContext, enemies, ExtraStrengthLoss,
                player.Creature, null);
        }

        if (!_enteredThisCombat)
        {
            _enteredThisCombat = true;
            await PowerCmd.Apply<IntangiblePower>(choiceContext, player.Creature, Intangible, player.Creature, null);
        }
    }
}
