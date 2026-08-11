using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 讨债人 — 女苑的能力。
/// 下一击清除目标 50% 王国资产，附加等量伤害并恢复等量生命（每层 +50%）。
/// 参考 VigorPower 的 BeforeAttack / ModifyDamageAdditive / AfterAttack 模式。
/// </summary>
public class DebtCollectorPower : TouhouAncientPowerModel
{
    public override string? CustomPackedIconPath => TouhouAncientCmd.CheckPathExists($"res://images/icon/power/RichestFormPower.png");
    public override string? CustomBigIconPath => TouhouAncientCmd.CheckPathExistsWithFallback2($"res://images/icon/power/BigIcon/RichestFormPower.png", CustomPackedIconPath);

    private class Data
    {
        public AttackCommand? trackedCommand;
        public readonly Dictionary<Creature, decimal> pendingRoyaltyLoss = new();
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker != base.Owner)
        {
            return Task.CompletedTask;
        }

        Data data = GetInternalData<Data>();
        data.trackedCommand = command;
        data.pendingRoyaltyLoss.Clear();
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != base.Owner || target == null)
        {
            return 0m;
        }

        RoyaltiesPower? royalties = target.GetPower<RoyaltiesPower>();
        if (royalties == null || royalties.Amount <= 0)
        {
            return 0m;
        }

        decimal bonus = Math.Floor(royalties.Amount * base.Amount / 100m);
        if (bonus > 0)
        {
            GetInternalData<Data>().pendingRoyaltyLoss[target] = bonus;
        }

        return bonus;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        Data data = GetInternalData<Data>();
        if (command != data.trackedCommand)
        {
            return;
        }

        foreach (KeyValuePair<Creature, decimal> kvp in data.pendingRoyaltyLoss)
        {
            await PowerCmd.Apply<RoyaltiesPower>(choiceContext, kvp.Key, -kvp.Value,
                base.Owner, null);

            // 讨债所得：恢复等量生命
            await CreatureCmd.Heal(base.Owner, kvp.Value);
        }

        data.pendingRoyaltyLoss.Clear();
        data.trackedCommand = null;

        await PowerCmd.Remove(this);
    }
}
