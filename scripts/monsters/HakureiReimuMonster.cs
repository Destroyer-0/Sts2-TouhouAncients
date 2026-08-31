using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using Godot;
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
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.powers;
using TouhouAncients.Scripts.Vfx;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 博丽灵梦：解决异变的红白巫女，符卡规则的制定者。
/// 状态机：梦想天生 → 梦想封印 → 八方鬼缚阵 → 阴阳玉 → 封魔针 → 降神 →（循环）梦想封印。
/// 固有能力"无差别降伏"触发时将意图切换至梦想天生并同步获得翱翔，释放梦想天生后移除翱翔，
/// 梦想天生后继续原先下一个意图。首次固定的梦想天生（战斗开场）同样在意图确定时进入翱翔。
/// </summary>
public sealed class HakureiReimuMonster : TouhouAncientMonsterBase
{
    // --- HP ---
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 186, 176);

    // --- 伤害/数值 ---
    private int DreamSealDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 14, 13);

    private int OctagonalBlock => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 32, 27);

    private int SealingNeedleDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 5, 4);

    private const int SealingNeedleHits = 3;

    private int TenbuHurricaneKickDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 20, 18);
    

    private const int SubspaceStrength = 2;

    /// <summary>降神使梦想天生计数（无差别降伏剩余计数）减少的层数。</summary>
    private const int SubspaceFantasyNatureCount = 3;

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

    /// <summary>灵符演出辅助（挂在场景根节点的脚本）。</summary>
    private HakureiReimuVisuals? _reimuVisuals;

    /// <summary>
    /// 梦想天生准备演出中禁止受击动画（jump_rise / spell_1 / spell_2 期间不播 hurt）。
    /// </summary>
    protected override void ConfigureAnimationStateMachine(MonsterAnimationStateMachine animationMachine)
    {
        // 准备演出期间全局禁止受击打断；演出结束（_isFantasyNaturePrepAnimating = false）后恢复
        animationMachine.DefaultCanBeInterruptedByHit = () => !_isFantasyNaturePrepAnimating;

        // 循环：idle / spell_2（准备姿态循环保持）/ jump_fall（落地循环）
        // 一次性：jump_rise / spell_1（播完显式切 spell_2）/ land
        animationMachine.RegisterLoop("idle");
        animationMachine.RegisterLoop("spell_2");
        animationMachine.RegisterLoop("jump_fall");
        animationMachine.RegisterOneShot("jump_rise");
        animationMachine.RegisterOneShot("spell_1");
        animationMachine.RegisterOneShot("land");
    }

    /// <summary>当前是否处于梦想天生准备演出（jump_rise → spell_1 → spell_2）中。</summary>
    private bool _isFantasyNaturePrepAnimating;

    /// <summary>
    /// 梦想天生演出期间的位移目标：准备演出时灵梦向上抬升 80px，攻击结束后落地回原位。
    /// </summary>
    private Vector2? _fantasyNatureLiftOffset;

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        _reimuVisuals = base.Creature.GetCreatureNode()?.Visuals as HakureiReimuVisuals;
        // 战斗开场：灵符整体隐藏，待首次梦想天生攻击结束后甩出
        if (_reimuVisuals?.AmuletRoot != null)
        {
            _reimuVisuals.AmuletRoot.Visible = false;
        }

        // 固有能力：无差别降伏（Counter 类型，层数即剩余计数）
        await PowerCmd.Apply<IndiscriminateSubjugationPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
        
        // 立即播放梦想天生准备演出（jump_rise 上移 → spell_1 → spell_2 循环保持），
        // 下回合发动梦想天生攻击时直接处于施法姿态
        await Cmd.Wait(0.5f);
        await PlayFantasyNaturePrepAnimation();
        
        // 首次固定的梦想天生（战斗开场第一回合）：意图确定为梦想天生时同步进入翱翔
        await PowerCmd.Apply<SoarPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    // --- 死亡演出 ---
    /// <summary>
    /// 灵梦死亡时，把 7 张灵符整体挂到当前身体下，
    /// 使其随身体一起被原版死亡风化特效（NMonsterDeathVfx 溶解）风化溶解，
    /// 而不是随节点被瞬间删除。
    /// </summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature == base.Creature)
        {
            NCreature? creatureNode = base.Creature.GetCreatureNode();
            HakureiReimuVisuals? visuals = creatureNode?.Visuals as HakureiReimuVisuals;
            Node2D? body = creatureNode?.Visuals.GetCurrentBody();
            if (visuals?.AmuletRoot != null && body != null)
            {
                visuals.ReparentAmuletTo(body);
            }
        }

        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
    }

    // --- 梦想天生准备演出 ---
    /// <summary>
    /// 梦想天生准备演出：先播放 jump_rise 动画并向上移动 80 像素，抵达目标位置后依次播放
    /// spell_1（不循环）与 spell_2（循环）。演出期间禁止受击动画（不播 hurt），
    /// 结束后保持 spell_2 循环姿态，等待梦想天生攻击（攻击落地后由 <see cref="FantasyNatureMove"/> 恢复）。
    /// </summary>
    private async Task PlayFantasyNaturePrepAnimation()
    {
        _isFantasyNaturePrepAnimating = true;
        NCreature? creatureNode = base.Creature.GetCreatureNode();
        Node2D? body = creatureNode?.Visuals.GetCurrentBody();
        Vector2 bodyOrigin = body?.Position ?? Vector2.Zero;
        _fantasyNatureLiftOffset = bodyOrigin + Vector2.Up * 100f;

        Anim.Trigger("jump_rise");
        if (body != null)
        {
            var riseTween = CreateBodyMoveTween(body);
            riseTween.TweenProperty(body, "position", _fantasyNatureLiftOffset.Value, 0.3f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            await Cmd.Wait(0.35f);
        }

        Anim.Trigger("spell_1");
        await Cmd.Wait(0.4f);
        Anim.Trigger("spell_2");
        await Cmd.Wait(0.2f);
    }

    // --- 无差别降伏：强制切换意图至梦想天生 ---
    /// <summary>
    /// 将当前意图立即切换至梦想天生，并保证梦想天生先执行一次，
    /// 执行完毕后回到触发切换前"原先的下一个意图"。
    /// 全部灵符激活后先收回，再播放准备演出（spell_2 姿态），切换意图后进入翱翔。
    /// </summary>
    internal async Task ForceDreamNatureNext()
    {
        if (_dreamNatureMove == null) return;

        // 全部灵符已激活：先渐隐并收回灵符，再摆出 spell_2 准备姿态
        await RetractAllAmulets();
        await PlayFantasyNaturePrepAnimation();

        // 保存当前技能的下一个意图：梦想天生执行后继续原序列
        MonsterState? originalNext = base.NextMove?.FollowUpState;
        if (originalNext != null)
        {
            _dreamNatureMove.FollowUpState = originalNext;
        }

        // 保证下回合先执行梦想天生（不跳过），执行后再转移
        _dreamNatureMove.MustPerformOnceBeforeTransitioning = true;
        base.SetMoveImmediate(_dreamNatureMove, forceTransition: true);

        // 进入梦想天生：同步获得翱翔
        await PowerCmd.Apply<SoarPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    // --- 状态机 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 梦想天生：动态伤害，单次 = max(2, floor(玩家当前生命/12))，共 6 段
        // 意图显示按每个目标玩家各自计算（DreamNatureIntent 内部按传入目标玩家取生命）
        MoveState dreamNature = new MoveState(FantasyNatureMoveId, FantasyNatureMove,
            new DreamNatureIntent());
//            new MultiAttackIntent(0, FantasyNatureHits));

        // 梦想封印：造成伤害并向每位玩家加入/升级状态卡牌
        MoveState dreamSeal = new MoveState("DREAM_SEAL", DreamSealMove,
            new SingleAttackIntent(DreamSealDamage), new StatusIntent(2));

        // 八方鬼缚阵：获得格挡 + 倒映 + 残影
        MoveState octagonal = new MoveState("OCTAGONAL_BINDING_ARRAY", OctagonalBindingArrayMove,
            new DefendIntent(), new BuffIntent());

        // 阴阳玉：造成伤害
        MoveState tenbu = new MoveState("TENBU_HURRICANE_KICK", TenbuHurricaneKickMove,
            new SingleAttackIntent(TenbuHurricaneKickDamage));

        // 封魔针：3 段攻击
        MoveState sealingNeedle = new MoveState("SEALING_NEEDLE", SealingNeedleMove,
            new MultiAttackIntent(SealingNeedleDamage, SealingNeedleHits));

        // 降神：获得力量并减少梦想天生计数
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
    /// 梦想天生：准备演出（jump_rise 上移 80px → spell_1 → spell_2 循环）已在
    /// <see cref="AfterAddedToRoom"/>（开场）或 <see cref="ForceDreamNatureNext"/>（计数归零）时播放完毕，
    /// 灵梦保持 spell_2 施法姿态，这里直接攻击（对每个目标玩家按其当前生命各自结算动态伤害，6 段），
    /// 攻击结束后落地（jump_fall → land → idle），并把 7 张灵符从自己身上甩出至目标位置。
    /// 释放梦想天生后移除进入时获得的翱翔。
    /// </summary>
    private async Task FantasyNatureMove(IReadOnlyList<Creature> targets)
    {
        CurrentMoveKey = FantasyNatureMoveId;

        // --- 攻击 ---
        //SfxCmd.Play(AttackSfx);
        await CreatureCmd.TriggerAnim(base.Creature, "Attack", 0.3f);

        var a = await PowerCmd.Apply<FakeDreamFantasyPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);

        if (a != null)
        {
            foreach (var target in targets)
            {
                a.SetCreatureHp(target, target.CurrentHp);
            }
        }

        await DamageCmd.Attack(0)
            .FromMonster(this)
            .WithHitCount(FantasyNatureHits)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await PowerCmd.Remove(a);

        // --- 落地演出：攻击完成，恢复受击动画 ---
        _isFantasyNaturePrepAnimating = false;
        _fantasyNatureLiftOffset = null;

        NCreature? creatureNode = base.Creature.GetCreatureNode();
        Node2D? body = creatureNode?.Visuals.GetCurrentBody();

        // jump_fall（循环）回到原位
        Anim.Trigger("jump_fall");
        if (body != null)
        {
            var fallTween = CreateBodyMoveTween(body);
            fallTween.TweenProperty(body, "position", Vector2.Zero, 0.3f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            await Cmd.Wait(0.35f);
        }

        // land（不循环）
        Anim.Trigger("land");
        await Cmd.Wait(0.3f);

        // 释放梦想天生后移除翱翔
        if (base.Creature.HasPower<SoarPower>())
        {
            await PowerCmd.Remove<SoarPower>(base.Creature);
        }
        
        // 回到 idle 并把 7 张灵符从自己身上甩出至目标位置
        Anim.TriggerLoop();
        await ThrowOutAllAmulets();
    }

    /// <summary>
    /// 梦想封印：造成伤害，并向每个目标玩家的弃牌堆加入 3 张"封魔阵"。
    /// 每张封魔阵加入后会自动寻找一张牌为其添加侵蚀"封印"并两两对应。
    /// </summary>
    private async Task DreamSealMove(IReadOnlyList<Creature> targets)
    {
        CurrentMoveKey = "DREAM_SEAL";

        await DamageCmd.Attack(DreamSealDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        foreach (Creature target in targets.Where(t => !t.IsDead))
        {
            Player? player = target.Player;
            if (player == null) continue;

            await CardPileCmd.AddToCombatAndPreview<SealCircle>(player.Creature, PileType.Discard, 3, null, CardPilePosition.Random);
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
    /// 阴阳玉：造成伤害。
    /// </summary>
    private async Task TenbuHurricaneKickMove(IReadOnlyList<Creature> targets)
    {
        CurrentMoveKey = "TENBU_HURRICANE_KICK";

        await DamageCmd.Attack(TenbuHurricaneKickDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
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
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>
    /// 降神：获得 2 点力量，并使梦想天生计数减少 3。
    /// </summary>
    private async Task SubspaceAcupressureMove(IReadOnlyList<Creature> targets)
    {
        CurrentMoveKey = "SUBSPACE_ACUPRESSURE";

        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, SubspaceStrength, base.Creature, null);
        // 使梦想天生计数（无差别降伏的剩余计数）减少 3；归零后由无差别降伏在造成伤害时触发意图切换
        var power = base.Creature.GetPower<IndiscriminateSubjugationPower>();
        if (power != null)
        {
            await power.DecreaseHitsLeft(SubspaceFantasyNatureCount);
        }
    }

    // --- 私有辅助 ---

    /// <summary>
    /// 甩出 7 张灵符：从灵梦中心（VfxSpawnPosition）飞向 Amulet1~7 的目标位置。
    /// 飞行途中灵符逐渐显现（alpha 0→1），方向跟随运动方向（旋转至运动角度），
    /// 抵达目标位置后旋转至 -90°（竖立），并保持未激活状态（红色 default 动画）。
    /// </summary>
    private async Task ThrowOutAllAmulets()
    {
        NCreature? creatureNode = base.Creature.GetCreatureNode();
        HakureiReimuVisuals? visuals = creatureNode?.Visuals as HakureiReimuVisuals;
        if (visuals?.AmuletRoot == null || visuals.AmuletRoot.IsInsideTree() == false)
        {
            return;
        }

        // 先显示根节点（灵符们从隐藏状态开始依次甩出）
        visuals.AmuletRoot.Visible = true;
        Vector2 start = creatureNode!.VfxSpawnPosition;


        for (int i = 0; i < 7; i++)
        {
            var amulet = visuals.GetAmulet(i).Sprite;
            if (amulet == null) continue;

            // 未激活：红色 default 动画
            amulet.Animation = "default";
            amulet.Visible = false;
            amulet.Modulate = new Color(1f, 1f, 1f, 0f);
            amulet.GlobalPosition = start;
            amulet.RotationDegrees = 0f;
        }
        
        for (int i = 0; i < 7; i++)
        {
            var data = visuals.GetAmulet(i);
            var amulet = visuals.GetAmulet(i).Sprite;
            Vector2 target = start+data.Offset;
            float distance = target.DistanceTo(start);
            float duration = Mathf.Clamp(distance / 2000f, 0.12f, 0.25f);

            amulet.Visible = true;
            // 方向跟随运动方向
            Vector2 dir = (target - start).Normalized();
            amulet.RotationDegrees = Mathf.RadToDeg(Mathf.Atan2(dir.Y, dir.X));

            var tween = amulet.CreateTween().SetParallel();
            tween.TweenProperty(amulet, "global_position", target, duration)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            tween.TweenProperty(amulet, "modulate:a", 1f, duration)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            if (await tween.AwaitFinished(amulet))
            {
                // 抵达目标位置后旋转至 -90°
                var rotateTween = amulet.CreateTween();
                rotateTween.TweenProperty(amulet, "rotation_degrees", -90f, 0.05f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
                await rotateTween.AwaitFinished(amulet);
            }
            await Cmd.Wait(0.04f);
        }
    }

    /// <summary>
    /// 无差别降伏计数减少时的回调：剩余计数越小，已激活的灵符越多。
    /// 剩余计数 7 → 0 张激活；剩余 0 → 7 张全部激活（从左到右依次激活 1~7 号）。
    /// </summary>
    public void NotifySubjugationHitsLeftChanged(int hitsLeft)
    {
        HakureiReimuVisuals? visuals = _reimuVisuals;
        if (visuals == null)
        {
            return;
        }

        int targetActivated = Math.Clamp(7 - hitsLeft, 0, 7);
        while (visuals.ActivatedCount < targetActivated)
        {
            ActivateAmulet(visuals.ActivatedCount);
        }
    }

    /// <summary>
    /// 激活第 index（0 基）张灵符：切换为橙色 trigger 动画（保持当前帧不变），
    /// 并在该灵符处播放一次符纸燃烧特效（amulet_orange2.png 逐帧动画）。
    /// 仅允许按顺序激活（index == 当前已激活数量），重复激活不会生效。
    /// </summary>
    public void ActivateAmulet(int index)
    {
        HakureiReimuVisuals? visuals = _reimuVisuals;
        if (visuals == null || index < 0 || index >= 7)
        {
            return;
        }
        // 顺序激活：只有下一张可以激活，已激活过的直接跳过
        if (index != visuals.ActivatedCount)
        {
            return;
        }

        var data = visuals.GetAmulet(index);
        var amulet = data.Sprite;
        if (amulet == null) return;

        if (amulet.Animation != "trigger")
        {
            // 保持当前帧数不变：先记录当前帧，切换动画名后恢复帧
            int currentFrame = amulet.Frame;
            amulet.Animation = "trigger";
            amulet.Frame = Mathf.Min(currentFrame, amulet.SpriteFrames.GetFrameCount("trigger") - 1);
        }

        // 播放符纸燃烧特效
        visuals.PlayBurnVfx(amulet.GlobalPosition);

        // 标记已激活数量 +1（保证后续激活从下一张开始）
        visuals.ActivatedCount = index + 1;
    }

    /// <summary>
    /// 全部灵符渐隐并被灵梦收回：先旋转至"朝向灵梦"角度，再飞回灵梦中心并淡出。
    /// 收回完成后隐藏整个 Amulet 根节点。
    /// </summary>
    private async Task RetractAllAmulets()
    {
        HakureiReimuVisuals? visuals = _reimuVisuals;
        NCreature? creatureNode = base.Creature.GetCreatureNode();
        if (visuals?.AmuletRoot == null || !visuals.AmuletRoot.IsInsideTree())
        {
            return;
        }

        Vector2 start = creatureNode?.VfxSpawnPosition ?? Vector2.Zero;

        for (int i = 6; i >= 0; i--)
        {
            var data = visuals.GetAmulet(i);
            var amulet = data.Sprite;
            if (amulet == null) return;

            // 先旋转至"朝向灵梦"角度
            Vector2 dir = (start - amulet.GlobalPosition).Normalized();
            var rotateTween = amulet.CreateTween();
            rotateTween.TweenProperty(amulet, "rotation_degrees", Mathf.RadToDeg(Mathf.Atan2(dir.Y, dir.X)), 0.05f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            await rotateTween.AwaitFinished(amulet);

            // 飞回灵梦中心并淡出
            var tween = amulet.CreateTween().SetParallel();
            tween.TweenProperty(amulet, "global_position", start, 0.1f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            tween.TweenProperty(amulet, "modulate:a", 0f, 0.1f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            await tween.AwaitFinished(amulet);
            amulet.Visible = false;
        }

        // 全部收回后隐藏根节点，等待下次甩出
        visuals.AmuletRoot.Visible = false;
        visuals.ResetActivatedCount();
    }
}
