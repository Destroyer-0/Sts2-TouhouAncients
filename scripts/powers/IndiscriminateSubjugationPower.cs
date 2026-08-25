using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 无差别降伏：每使用梦想天生以外的技能造成 7 次未被格挡的伤害，下一次意图切换至梦想天生。
/// 倒计时显示与凋萎存在（WitheringPresencePower）一致：图标上直接显示剩余次数。
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

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != base.Owner) return;
        // "未被格挡"判定：本次命中实际穿透格挡造成了伤害
        if (result.UnblockedDamage <= 0) return;
        //if (!props.IsCardOrMonsterMove()) return;
        if (base.Owner.Monster is not HakureiReimuMonster reimu) return;
        // 梦想天生造成的伤害不计数（每段命中都会触发本 Hook，封魔针 7×3 的三段命中算三次）
        if (reimu.CurrentMoveKey == "FANTASY_NATURE") return;
        if (!shouldTriggerThisTurn) return;
        base.DynamicVars[_hitsLeftKey].BaseValue--;
        InvokeDisplayAmountChanged();
        reimu.NotifySubjugationHitsLeftChanged(GetHitsLeft());
        await TryTrigger();
    }

    /// <summary>
    /// 减少梦想天生计数（HitsLeft）：「降神」等效果使用。
    /// 计数不会低于 0；归零后仍由 <see cref="AfterDamageGiven"/> 在造成伤害时触发意图切换。
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