using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 无差别降伏：每使用梦想天生以外的技能造成 7 次未被格挡的伤害，下一次意图切换至梦想天生。
/// 倒计时显示与凋萎存在（WitheringPresencePower）一致：图标上直接显示剩余次数。
/// 多人模式下按"段"计数（参考化石追踪者的吮吸 SuckPower）：一段攻击内无论命中多少个玩家，
/// 只要其中任一玩家受到未被格挡的伤害就只计 1 次，而不是按玩家数量累加。
/// </summary>
public sealed class IndiscriminateSubjugationPower : TouhouAncientPowerModel
{
    private const int _baseHitsLeft = 7;

    private const string _hitsLeftKey = "HitsLeft";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => base.DynamicVars[_hitsLeftKey].IntValue;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(_hitsLeftKey, _baseHitsLeft)];

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        // 仅统计灵梦自己的攻击
        if (command.Attacker != base.Owner) return;
        if (base.Owner.Monster is not HakureiReimuMonster reimu) return;
        // 梦想天生造成的伤害不计数
        if (reimu.CurrentMoveKey == "FANTASY_NATURE") return;
        if (!shouldTriggerThisTurn) return;

        // 参考 SuckPower.AfterAttack：按"段"计数。
        // 每段（List<DamageResult>）内只要有任一目标玩家受到未被格挡的伤害就计 1 次，
        // 一段攻击命中多个玩家时只计 1 次（多人模式下按最大计数而非累加）。
        int hits = 0;
        foreach (List<DamageResult> hit in command.Results)
        {
            if (hit.Any(r => r.UnblockedDamage > 0))
            {
                hits++;
            }
        }
        if (hits <= 0) return;

        base.DynamicVars[_hitsLeftKey].BaseValue = Math.Max(0, base.DynamicVars[_hitsLeftKey].IntValue - hits);
        InvokeDisplayAmountChanged();
        reimu.NotifySubjugationHitsLeftChanged(GetHitsLeft());
        await TryTrigger();
    }

    /// <summary>
    /// 减少梦想天生计数（HitsLeft）：「降神」等效果使用。
    /// 计数不会低于 0；归零后仍由 <see cref="AfterAttack"/> 在造成伤害时触发意图切换。
    /// </summary>
    public async Task DecreaseHitsLeft(int amount)
    {
        if (!shouldTriggerThisTurn) return;
        base.DynamicVars[_hitsLeftKey].BaseValue = Math.Max(0, base.DynamicVars[_hitsLeftKey].IntValue - amount);
        InvokeDisplayAmountChanged();
        if (base.Owner.Monster is HakureiReimuMonster reimu)
        {
            reimu.NotifySubjugationHitsLeftChanged(GetHitsLeft());
        }
        await TryTrigger();
    }

    /// <summary>当前剩余计数。</summary>
    private int GetHitsLeft() => base.DynamicVars[_hitsLeftKey].IntValue;

    private bool shouldTriggerThisTurn = true;

    public async Task TryTrigger()
    {
        if (base.DynamicVars[_hitsLeftKey].IntValue <= 0 && base.Owner.Monster is HakureiReimuMonster reimu)
        {
            // 等待本次命中特效播放完毕后，将意图切换至梦想天生（并同步进入翱翔）
            shouldTriggerThisTurn = false;
            await Cmd.Wait(0.5f);
            Flash();
            await reimu.ForceDreamNatureNext();
            base.DynamicVars[_hitsLeftKey].BaseValue = _baseHitsLeft;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
        {
            shouldTriggerThisTurn = true;
        }
        return base.AfterSideTurnEnd(choiceContext, side, participants);
    }
}