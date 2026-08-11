using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 博丽灵梦：解决异变的红白巫女，符卡规则的制定者。
/// 状态机：梦想天生 → 梦想封印 → 八方鬼缚阵 → 天霸风神脚 → 封魔针 → 亚空点穴 →（循环）梦想封印。
/// 固有能力"无差别降伏"触发时将意图切换至梦想天生，梦想天生后继续原先下一个意图。
/// </summary>
public sealed class HakureiReimuMonster : TouhouAncientMonsterBase
{
    // --- HP ---
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 206, 196);

    // --- 伤害/数值 ---
    private int DreamSealDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 19, 17);

    private int OctagonalBlock => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 32, 27);

    private int SealingNeedleDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 8, 7);

    private const int SealingNeedleHits = 3;

    private int TenbuHurricaneKickDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 9, 8);
    
    private const int TenbuHurricaneKickHits = 2;

    private const int SubspaceIntangible = 1;

    private const int SubspaceStrength = 2;

    private const int FantasyNatureHits = 6;

    private const string FantasyNatureMoveId = "FANTASY_NATURE";

    /// <summary>
    /// 梦想天生 MoveState 引用（无差别降伏强制切换意图用）。
    /// 在 <see cref="GenerateMoveStateMachine"/> 中赋值。
    /// </summary>
    private MoveState? _dreamNatureMove;

    /// <summary>
    /// 当前正在执行的技能 Key。每个 Move 方法开始时设置，
    /// 供 <see cref="IndiscriminateSubjugationPower"/> 判断"梦想天生以外的技能"。
    /// </summary>
    public string? CurrentMoveKey { get; private set; }

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // 固有能力：无差别降伏（Counter 类型，层数即剩余计数）
        await PowerCmd.Apply<IndiscriminateSubjugationPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    // --- 翱翔落地：灵梦下回合行动时移除 ---
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != base.Creature.Side) return;
        if (!participants.Contains(Creature)) return;
        if (CurrentMoveKey == "TENBU_HURRICANE_KICK") return;
        if (base.Creature.HasPower<SoarPower>())
        {
            await PowerCmd.Remove<SoarPower>(base.Creature);
        }
    }

    // --- 无差别降伏：强制切换意图至梦想天生 ---
    /// <summary>
    /// 将当前意图立即切换至梦想天生，并保证梦想天生先执行一次，
    /// 执行完毕后回到触发切换前"原先的下一个意图"。
    /// </summary>
    internal void ForceDreamNatureNext()
    {
        if (_dreamNatureMove == null) return;

        // 保存当前技能的下一个意图：梦想天生执行后继续原序列
        MonsterState? originalNext = base.NextMove?.FollowUpState;
        if (originalNext != null)
        {
            _dreamNatureMove.FollowUpState = originalNext;
        }

        // 保证下回合先执行梦想天生（不跳过），执行后再转移
        _dreamNatureMove.MustPerformOnceBeforeTransitioning = true;
        base.SetMoveImmediate(_dreamNatureMove, forceTransition: true);
    }

    // --- 状态机 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 梦想天生：动态伤害，单次 = max(2, floor(玩家当前生命/12))，共 6 段
        // 意图显示按每个目标玩家各自计算（DreamNatureIntent 内部按传入目标玩家取生命）
        MoveState dreamNature = new MoveState(FantasyNatureMoveId, FantasyNatureMove,
            new DreamNatureIntent());

        // 梦想封印：造成伤害并向每位玩家加入/升级状态卡牌
        MoveState dreamSeal = new MoveState("DREAM_SEAL", DreamSealMove,
            new SingleAttackIntent(DreamSealDamage), new StatusIntent(2));

        // 八方鬼缚阵：获得格挡 + 倒映 + 残影
        MoveState octagonal = new MoveState("OCTAGONAL_BINDING_ARRAY", OctagonalBindingArrayMove,
            new DefendIntent(), new BuffIntent());

        // 天霸风神脚：造成伤害并进入翱翔（下回合落地）
        MoveState tenbu = new MoveState("TENBU_HURRICANE_KICK", TenbuHurricaneKickMove,
            new MultiAttackIntent(TenbuHurricaneKickDamage, TenbuHurricaneKickHits), new BuffIntent());

        // 封魔针：3 段攻击
        MoveState sealingNeedle = new MoveState("SEALING_NEEDLE", SealingNeedleMove,
            new MultiAttackIntent(SealingNeedleDamage, SealingNeedleHits));

        // 亚空点穴：获得无实体与力量
        MoveState subspace = new MoveState("SUBSPACE_ACUPRESSURE", SubspaceAcupressureMove,
            new BuffIntent());

        _dreamNatureMove = dreamNature;

        // 固定序列（第一回合梦想天生，之后从梦想封印循环）
        dreamNature.FollowUpState = dreamSeal;
        dreamSeal.FollowUpState = octagonal;
        octagonal.FollowUpState = tenbu;
        tenbu.FollowUpState = sealingNeedle;
        sealingNeedle.FollowUpState = subspace;
        subspace.FollowUpState = dreamSeal;

        List<MonsterState> list = new List<MonsterState>
        {
            dreamNature, dreamSeal, octagonal, tenbu, sealingNeedle, subspace
        };

        return new MonsterMoveStateMachine(list, dreamNature);
    }

    // --- 技能方法 ---

    /// <summary>
    /// 梦想天生：对每个目标玩家按其当前生命各自结算动态伤害（6 段）。
    /// 单次伤害 = max(2, floor(玩家当前生命 / 12))，享受力量加成。
    /// 说明：AttackCommand.FromMonster 固定攻击全体玩家且伤害统一，无法按玩家分别结算，
    /// 因此这里使用 CreatureCmd.Damage 逐玩家逐段执行，手动播放攻击动画与命中特效。
    /// </summary>
    private async Task FantasyNatureMove(IReadOnlyList<Creature> targets)
    {
        CurrentMoveKey = FantasyNatureMoveId;

        SfxCmd.Play(AttackSfx);
        await CreatureCmd.TriggerAnim(base.Creature, "Attack", 0.3f);

        foreach (Creature target in targets.Where(t => !t.IsDead))
        {
            decimal damage = Math.Max(2m, Math.Floor(target.CurrentHp / 12m));
            for (int i = 0; i < FantasyNatureHits; i++)
            {
                VfxCmd.PlayOnCreature(target, "vfx/vfx_attack_slash");
                await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), target, damage, ValueProp.Move, base.Creature);
                await Cmd.Wait(0.08f);
            }
        }
    }

    /// <summary>
    /// 梦想封印：造成伤害，并向每个目标玩家的抽牌堆加入"梦想封印·侘"与"梦想封印·寂"。
    /// 若该牌已存在于玩家牌堆中，则将其移动至抽牌堆并升级。
    /// </summary>
    private async Task DreamSealMove(IReadOnlyList<Creature> targets)
    {
        CurrentMoveKey = "DREAM_SEAL";

        await DamageCmd.Attack(DreamSealDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        foreach (Creature target in targets.Where(t => !t.IsDead))
        {
            Player? player = target.Player;
            if (player == null) continue;

            await AddOrUpgradeDreamSeal<DreamSealWabi>(player);
            await AddOrUpgradeDreamSeal<DreamSealSabi>(player);
        }
    }

    /// <summary>
    /// 八方鬼缚阵：获得格挡，以及 1 层倒映与 1 层残影。
    /// </summary>
    private async Task OctagonalBindingArrayMove(IReadOnlyList<Creature> targets)
    {
        CurrentMoveKey = "OCTAGONAL_BINDING_ARRAY";

        await CreatureCmd.GainBlock(base.Creature, OctagonalBlock, ValueProp.Move, null);
        await PowerCmd.Apply<ReflectPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
        await PowerCmd.Apply<BlurPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    /// <summary>
    /// 天霸风神脚：造成伤害并进入翱翔（下回合灵梦行动时落地）。
    /// </summary>
    private async Task TenbuHurricaneKickMove(IReadOnlyList<Creature> targets)
    {
        CurrentMoveKey = "TENBU_HURRICANE_KICK";

        await DamageCmd.Attack(TenbuHurricaneKickDamage)
            .FromMonster(this)
            .WithHitCount(TenbuHurricaneKickHits)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await PowerCmd.Apply<SoarPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    /// <summary>
    /// 封魔针：3 段攻击。
    /// </summary>
    private async Task SealingNeedleMove(IReadOnlyList<Creature> targets)
    {
        CurrentMoveKey = "SEALING_NEEDLE";

        await DamageCmd.Attack(SealingNeedleDamage)
            .FromMonster(this)
            .WithHitCount(SealingNeedleHits)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>
    /// 亚空点穴：获得 1 层无实体与 2 点力量。
    /// </summary>
    private async Task SubspaceAcupressureMove(IReadOnlyList<Creature> targets)
    {
        CurrentMoveKey = "SUBSPACE_ACUPRESSURE";

        await PowerCmd.Apply<IntangiblePower>(new ThrowingPlayerChoiceContext(), base.Creature, SubspaceIntangible, base.Creature, null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, SubspaceStrength, base.Creature, null);
    }

    // --- 私有辅助 ---

    /// <summary>
    /// 对指定玩家执行"梦想封印·X"的加入/升级逻辑：
    /// 若抽牌堆/手牌/弃牌堆/消耗堆中不存在该牌，生成并放入抽牌堆；
    /// 否则将该牌移动至抽牌堆并升级。
    /// </summary>
    private async Task AddOrUpgradeDreamSeal<T>(Player player) where T : ReimuBossDreamSealStatus
    {
        ReimuBossDreamSealStatus? existing = FindDreamSealInCombatPiles<T>(player);
        if (existing == null)
        {
            // 不存在：生成并放入抽牌堆
            //var newCard = player.RunState.CreateCard(ModelDb.Card<T>(), player);
            
            await CardPileCmd.AddToCombatAndPreview<T>(player.Creature, PileType.Draw,1, null,CardPilePosition.Random);
           // await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Draw, null, CardPilePosition.Random);
            //await CardPileCmd.Add(newCard, PileType.Draw);
        }
        else
        {
            // 存在：移动至抽牌堆并升级
            await CardPileCmd.Add(existing, PileType.Draw);
            existing.FakeUpgrade();
        }
    }

    /// <summary>
    /// 在玩家的抽牌堆/手牌/弃牌堆/消耗堆中查找指定类型的卡牌实例。
    /// </summary>
    private static ReimuBossDreamSealStatus? FindDreamSealInCombatPiles<T>(Player player) where T : ReimuBossDreamSealStatus
    {
        foreach (PileType pileType in new[] { PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust })
        {
            var  found = pileType.GetPile(player).Cards.FirstOrDefault(c => c is T);
            if (found != null) return (ReimuBossDreamSealStatus)found;
        }
        return null;
    }
}
