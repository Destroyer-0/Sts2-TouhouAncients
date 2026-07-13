using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 舞台装置：
/// 你的回合开始时，给予所有敌人1层易伤，如果已存在拥有易伤的敌人，失去1点生命。
/// 你的回合结束时，给予所有敌人1层虚弱，如果已存在拥有虚弱的敌人，失去1点生命。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class StageDevice : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("VulnAmount", 1),
        new DynamicVar("WeakAmount", 1),
        new DynamicVar("TempStr", 2),
        new DynamicVar("TempDex", 2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != base.Owner.Creature.Side) return;

        if (!participants.Contains(Owner.Creature))
        {
            return ;
        }
        Flash();

        var playerCreature = base.Owner.Creature;
        var enemies = combatState.GetOpponentsOf(playerCreature).Where(c => c.IsAlive).ToList();
        if (enemies.Count == 0) return;

        int round = combatState.RoundNumber;

        if (round % 2 == 1) // 奇数回合：临时力量 + 易伤
        {
            await PowerCmd.Apply<StageDeviceStrengthPower>(new ThrowingPlayerChoiceContext(), playerCreature,
                base.DynamicVars["TempStr"].BaseValue, playerCreature, null);
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), enemies,
                base.DynamicVars["VulnAmount"].BaseValue, playerCreature, null);
        }
        else // 偶数回合：临时敏捷 + 虚弱
        {
            await PowerCmd.Apply<StageDeviceDexterityPower>(new ThrowingPlayerChoiceContext(), playerCreature,
                base.DynamicVars["TempDex"].BaseValue, playerCreature, null);
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), enemies,
                base.DynamicVars["WeakAmount"].BaseValue, playerCreature, null);
        }
    }
}