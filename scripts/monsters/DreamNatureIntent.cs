using System;
using System.Linq;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 梦想天生的动态伤害意图：单次伤害 = max(2, floor(玩家当前生命 / 12))，共 6 段。
/// 伤害享受力量加成（GetSingleDamage 内部走 Hook.ModifyDamage 管线自动应用，无需额外处理）。
/// 注意：此意图仅用于显示，实际伤害在 <see cref="HakureiReimuMonster.FantasyNatureMove"/>
/// 中按每个目标玩家当前生命各自结算。
/// 每个玩家客户端各自计算：基础伤害取"本地玩家的当前生命"（LocalContext.GetMe），
/// 因此不同玩家看到自己生命对应的伤害数值，而不是全体统一的数值。
/// </summary>
public sealed class DreamNatureIntent : MultiAttackIntent
{
    /// <summary>意图所属的怪物 Creature（用于解析本地玩家）。在 GetTotalDamage / GetIntentLabel 时记录。</summary>
    private Creature _owner = null!;

    public DreamNatureIntent() : base(2, 6)
    {
        // DamageCalc 为 protected setter，子类可访问。
        // 基类 GetSingleDamage 会用此委托计算基础伤害并应用力量修正，
        // 而此委托在调用前已被更新为按本地玩家当前生命计算的值。
        base.DamageCalc = () => DamageForLocalPlayer(_owner);
    }

    /// <summary>
    /// 按本地玩家当前生命计算单次伤害：max(2, floor(本地玩家当前生命 / 12))。
    /// 本地玩家解析失败（如单人模式判定）时取传入目标中生命最高者兜底。
    /// </summary>
    private static decimal DamageForLocalPlayer(Creature owner)
    {
        if (owner?.CombatState != null && LocalContext.GetMe(owner.CombatState) is { } me)
        {
            return Math.Max(2m, Math.Ceiling(me.Creature.CurrentHp / 12m));
        }
        return 2m;
    }

    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner)
    {
        _owner = owner;
        return base.GetTotalDamage(targets, owner);
    }

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        _owner = owner;
        return base.GetIntentLabel(targets, owner);
    }
}
