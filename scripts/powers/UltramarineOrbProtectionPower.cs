using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.Ui;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 绀珠之庇：受到致命伤害时改为临时失去最大生命值并回复生命；
/// 打出孤注一掷后，它在下回合开始时返回手牌；战斗结束时返还临时失去的最大生命。
/// 层数耗尽后仍保留图标（只隐藏免死说明），便于查看累计损失。
/// </summary>
public sealed class UltramarineOrbProtectionPower : TouhouAncientPowerModel
{
    private const decimal MaxHpRetainRate = 0.5m;

    /// <summary>已打出、等待下回合返回手牌的孤注一掷。</summary>
    private readonly List<CardModel> _returningGambits = new();

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("LostMaxHp", 0)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<TheGambit>();

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != base.Owner) return true;
        return base.Amount <= 0;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        JunkoLifeMarkerDisplay.Attach(base.Owner, base.Amount);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        JunkoLifeMarkerDisplay.Detach(oldOwner);
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (power != this) return;
        JunkoLifeMarkerDisplay.Sync(base.Owner, base.Amount);
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != base.Owner) return;

        Flash();
        // 直接取损失后的 50%：至少保留 1 点最大生命
        int retainedMaxHp = Math.Max(1, (int)Math.Floor(creature.MaxHp * MaxHpRetainRate));
        int lostMaxHp = creature.MaxHp - retainedMaxHp;

        await CreatureCmd.SetMaxHp(creature, retainedMaxHp);
        await CreatureCmd.Heal(creature, creature.MaxHp - creature.CurrentHp);

        if (lostMaxHp > 0)
        {
            base.DynamicVars["LostMaxHp"].BaseValue += lostMaxHp;
            InvokeDisplayAmountChanged();
        }

        // 不走 PowerCmd 降层：层数归零时该能力会被移除，0 层仍需保留图标与累计信息
        SetAmount(base.Amount - 1);
        // SetAmount 只触发 Creature 事件，不跑 AfterPowerAmountChanged 钩子，需手动刷新标记
        JunkoLifeMarkerDisplay.Sync(base.Owner, base.Amount);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card is not TheGambit gambit) return Task.CompletedTask;
        if (gambit.Owner != base.Owner.Player) return Task.CompletedTask;
        if (!_returningGambits.Contains(gambit)) _returningGambits.Add(gambit);
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(base.Owner)) return;
        if (_returningGambits.Count == 0) return;

        List<CardModel> returning = [.. _returningGambits];
        _returningGambits.Clear();
        foreach (CardModel card in returning)
        {
            if (card.HasBeenRemovedFromState) continue;
            if (card.Pile?.Type == PileType.Hand) continue;
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top);
        }
    }

    /// <summary>
    /// 战斗结束返还临时失去的最大生命上限。AfterCombatEnd 早于奖励生成，
    /// 因此挑战回血奖励等后续结算看到的是恢复后的上限。
    /// </summary>
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        int lostMaxHp = (int)base.DynamicVars["LostMaxHp"].BaseValue;
        if (lostMaxHp <= 0 || base.Owner.IsDead) return;

        base.DynamicVars["LostMaxHp"].BaseValue = 0;
        await CreatureCmd.SetMaxHp(base.Owner, base.Owner.MaxHp + lostMaxHp);
    }
}
