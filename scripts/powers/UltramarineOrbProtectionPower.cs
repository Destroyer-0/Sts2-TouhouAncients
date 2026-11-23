using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
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

    /// <summary>致死预警缓存：Hook 只置脏，推演推迟到 UI 读取时。</summary>
    private bool _lethalDirty = true;
    private bool _lethalCached;

    /// <summary>上次读取时的意图指纹（NextMove 引用 + 总段数），用于发现击晕等无 Hook 的换招。</summary>
    private readonly List<(MoveState? move, int hits)> _intentSignature = new();

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
        _lethalDirty = true;
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (power != this) return;
        JunkoLifeMarkerDisplay.Sync(base.Owner, base.Amount);
    }

    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature == base.Owner) _lethalDirty = true;
        return Task.CompletedTask;
    }

    public override Task AfterBlockCleared(Creature creature)
    {
        if (creature == base.Owner) _lethalDirty = true;
        return Task.CompletedTask;
    }

    public override Task AfterBlockBroken(PlayerChoiceContext choiceContext, Creature target, Creature? breaker)
    {
        if (target == base.Owner) _lethalDirty = true;
        return Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == base.Owner) _lethalDirty = true;
        return Task.CompletedTask;
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
        // 回合切换会重掷意图，同时兜住格挡部分消耗 / 能力移除等没有 Hook 的变化
        _lethalDirty = true;
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

    /// <summary>
    /// 推演敌人下一次攻击：本次攻击是否会让该玩家真正死亡（爱心耗尽后仍被击杀）。
    /// 只供 UI 预警，不改动任何状态；其它免死来源不计入。
    /// </summary>
    public bool IsLethalIncoming
    {
        get
        {
            // canonical 实例没有 Owner，预览 UI 枚举能力时不能读
            if (base.IsCanonical) return false;
            if (base.Owner.IsDead || base.Owner.CombatState is not { } combatState) return false;
            if (_lethalDirty || IntentsChanged(combatState))
            {
                _lethalDirty = false;
                _lethalCached = ComputeLethal(combatState);
            }
            return _lethalCached;
        }
    }

    private bool ComputeLethal(ICombatState combatState)
    {
        Creature creature = base.Owner;
        if (creature.Player is not { } player) return false;

        int hp = creature.CurrentHp;
        int block = creature.Block;
        int maxHp = creature.MaxHp;
        int revives = (int)base.Amount;
        bool gambit = creature.GetPower<TheGambitPower>() is not null;

        IReadOnlyList<Creature> enemies = combatState.Enemies;
        for (int e = 0; e < enemies.Count; e++)
        {
            Creature enemy = enemies[e];
            if (enemy.IsDead || enemy.Monster is not { } monster) continue;

            IReadOnlyList<AbstractIntent> intents = monster.NextMove.Intents;
            for (int n = 0; n < intents.Count; n++)
            {
                if (intents[n] is not AttackIntent { DamageCalc: { } damageCalc } intent) continue;

                // 走完整 Hook 管线：力量/虚弱/易伤/无实体上限都在这里面
                decimal perHit = Hook.ModifyDamage(player.RunState, combatState, creature, enemy, damageCalc(),
                    ValueProp.Move, null, null, ModifyDamageHookType.All, CardPreviewMode.None, out _);
                if (perHit <= 0m) continue;

                for (int i = 0, hits = Math.Max(1, intent.Repeats); i < hits; i++)
                {
                    // 与 DamageBlockInternal / LoseHpInternal 的取整方式保持一致
                    decimal blocked = Math.Min(block, perHit);
                    block -= (int)blocked;
                    decimal unblocked = perHit - blocked;
                    if (unblocked <= 0m) continue;

                    if (!gambit && (int)unblocked < hp)
                    {
                        hp -= (int)unblocked;
                        continue;
                    }

                    if (revives <= 0) return true;

                    revives--;
                    gambit = false; // 孤注一掷触发后原版会移除该能力
                    maxHp = Math.Max(1, (int)Math.Floor(maxHp * MaxHpRetainRate));
                    hp = maxHp;
                }
            }
        }

        return false;
    }

    /// <summary>意图变化没有 Hook（击晕走 SetMoveImmediate），只能用指纹比对发现。</summary>
    private bool IntentsChanged(ICombatState combatState)
    {
        bool changed = false;
        int index = 0;
        IReadOnlyList<Creature> enemies = combatState.Enemies;
        for (int e = 0; e < enemies.Count; e++)
        {
            Creature enemy = enemies[e];
            if (enemy.IsDead) continue;

            MoveState? move = enemy.Monster?.NextMove;
            int hits = 0;
            if (move != null)
            {
                IReadOnlyList<AbstractIntent> intents = move.Intents;
                for (int n = 0; n < intents.Count; n++)
                {
                    if (intents[n] is AttackIntent attack) hits += Math.Max(1, attack.Repeats);
                }
            }

            if (index < _intentSignature.Count)
            {
                if (!ReferenceEquals(_intentSignature[index].move, move) || _intentSignature[index].hits != hits)
                {
                    _intentSignature[index] = (move, hits);
                    changed = true;
                }
            }
            else
            {
                _intentSignature.Add((move, hits));
                changed = true;
            }
            index++;
        }

        if (_intentSignature.Count != index)
        {
            _intentSignature.RemoveRange(index, _intentSignature.Count - index);
            changed = true;
        }
        return changed;
    }
}
