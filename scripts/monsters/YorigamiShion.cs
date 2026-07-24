using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

public sealed class YorigamiShion : CustomMonsterModel
{
    // --- 本地化 ---
    private static readonly LocString _absoluteLoserLine =
        new LocString("monsters", "TOUHOUANCIENTS-YORIGAMI_SHION.moves.ABSOLUTE_LOSER.banter");

    // --- HP ---
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 110, 100);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 110, 100);

    // --- 伤害/数值 ---
    private int DoomSpreadDoom => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 5, 4);

    private int DoomSpreadWeak => 1;

    private int FeatherPluckingDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 8, 7);

    private int AbsoluteLoserDoom => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 18, 16);

    private int AbsoluteLoserRoyaltiesLoss => 10;

    public override NCreatureVisuals? CreateCustomVisuals() =>
        NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/creature_visuals/YorigamiShion.tscn");


    // --- 音效 ---
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    // --- 状态（延迟初始化） ---
    public MoveState StunnedState;

    public MoveState SelfRepairState;


    private ConditionalBranchState _rootBranch;

    /// <summary>
    /// 退出自修复状态，切回根条件分支重新评估（显示正常意图）。
    /// </summary>
    public void ExitSelfRepairState()
    {
        SelfRepairState!.FollowUpState = _rootBranch;
        SetMoveImmediate(SelfRepairState, forceTransition: true);
        PlayAnim("idle_loop");
    }

    public override bool ShouldFadeAfterDeath => false;
    public override bool ShouldDisappearFromDoom => false;


    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        AnimatedSprite2D.AnimationFinished += OnAnimationFinished;
        await PowerCmd.Apply<UnfortunatePower>(new ThrowingPlayerChoiceContext(), base.Creature, 5m, base.Creature,
            null);
        await PowerCmd.Apply<TwinSoulPower>(new ThrowingPlayerChoiceContext(), base.Creature, 8m, base.Creature, null);
    }

    // --- 状态机 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();

        // 阶段1：厄运传播 → 雁过拔毛（循环）
        MoveState doomSpread = new MoveState("DOOM_SPREAD", DoomSpreadMove,
            new DebuffIntent());
        MoveState featherPlucking = new MoveState("FEATHER_PLUCKING", FeatherPluckingMove,
            new SingleAttackIntent(FeatherPluckingDamage));

        doomSpread.FollowUpState = featherPlucking;
        featherPlucking.FollowUpState = doomSpread;

        // 阶段2：绝对输家（自身循环）
        MoveState absoluteLoser = new MoveState("ABSOLUTE_LOSER", AbsoluteLoserMove, new DebuffIntent());
        absoluteLoser.FollowUpState = absoluteLoser;

        StunnedState = new MoveState("STUNNED", StunnedMove, new StunIntent());
        // 眩晕结束后进入阶段2
        StunnedState.FollowUpState = absoluteLoser;

        SelfRepairState = new MoveState("SELF_REPAIR", SelfRepairMove, new HealIntent());
        // 默认自循环（击倒后等待复活），ExitSelfRepairState 中会改为 _rootBranch
        SelfRepairState.FollowUpState = SelfRepairState;

        // 根条件分支：女苑存活 → 阶段1，否则 → 阶段2
        _rootBranch = new ConditionalBranchState("ROOT");
        _rootBranch.AddState(doomSpread, IsJoonAlive);
        _rootBranch.AddState(absoluteLoser, () => !IsJoonAlive());

        list.Add(doomSpread);
        list.Add(featherPlucking);
        list.Add(absoluteLoser);
        list.Add(StunnedState);
        list.Add(SelfRepairState);
        list.Add(_rootBranch);

        return new MonsterMoveStateMachine(list, doomSpread);
    }


    private void OnAnimationFinished()
    {
        if (AnimatedSprite2D.Animation == "hurt")
        {
            AnimatedSprite2D.Play(IsJoonAlive() ? "idle_loop" : "spell");
        }
    }

    /// <summary>
    /// 检测女苑是否存活。
    /// </summary>
    private bool IsJoonAlive()
    {
        return base.CombatState.Enemies.Any(c => c.Monster is YorigamiJoon && !c.IsDead);
    }

    /// <summary>
    /// 女苑死亡时：紫苑移除双生能力，本回合眩晕，下一回合切换到阶段2。
    /// 紫苑自身死亡时由 TwinSoulPower 处理复活。
    /// </summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature == base.Creature) return;
        if (creature.Monster is YorigamiJoon)
        {
            await PowerCmd.Remove<TwinSoulPower>(base.Creature);
            await CreatureCmd.Stun(creature, StunnedMove, StunnedState.StateId);
            PlayAnim("die");
            SetMoveImmediate(StunnedState);
        }
    }

    public void SetSelfRepairState()
    {
        PlayAnim("damage");
        SetMoveImmediate(SelfRepairState);
    }

    /// <summary>
    /// 受击时播放一轮 hurt 动画后回到 idle。
    /// </summary>
    // public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
    //     DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    // {
    //     if (target == base.Creature && props.IsCardOrMonsterMove())
    //     {
    //         PlayAnim("hurt");
    //         await Cmd.Wait(1f);
    //         PlayAnim("idle_loop");
    //     }
    // }

    // --- 技能方法 ---
    private AnimatedSprite2D? _animatedSprite2D;

    public AnimatedSprite2D AnimatedSprite2D
    {
        get
        {
            if (_animatedSprite2D == null)
            {
                var body = base.Creature.GetCreatureNode()?.Visuals.GetCurrentBody();
                if (body is AnimatedSprite2D sprite)
                {
                    _animatedSprite2D = sprite;
                }
            }

            return _animatedSprite2D;
        }
    }

    /// <summary>
    /// 直接控制 AnimatedSprite2D 播放指定动画。
    /// </summary>
    private void PlayAnim(string animationName)
    {
        AnimatedSprite2D.Animation = animationName;
        AnimatedSprite2D.Play();
    }

    /// <summary>
    /// 厄运传播：给予 1 虚弱 + 灾厄。
    /// </summary>
    private async Task DoomSpreadMove(IReadOnlyList<Creature> targets)
    {
        PlayAnim("spell");
        await Cmd.Wait(0.5f);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, DoomSpreadWeak, base.Creature,
            null);
        await PowerCmd.Apply<DoomPower>(new ThrowingPlayerChoiceContext(), targets, DoomSpreadDoom, base.Creature,
            null);
        await Cmd.Wait(1f);
        PlayAnim("idle_loop");
    }

    /// <summary>
    /// 雁过拔毛：Rush 穿过玩家，离开屏幕左侧后从右侧返回，造成伤害。
    /// </summary>
    private async Task FeatherPluckingMove(IReadOnlyList<Creature> targets)
    {
        PlayAnim("rush");

        // 找到最左侧玩家
        Vector2? targetPos = null;
        foreach (Creature target in targets)
        {
            NCreature creatureNode = target.GetCreatureNode();
            if (creatureNode != null && (!targetPos.HasValue || targetPos.Value.X > creatureNode.GlobalPosition.X))
            {
                targetPos = creatureNode.GlobalPosition;
            }
        }

        NCreature myNode = base.Creature.GetCreatureNode();
        Node2D body = myNode?.Visuals.GetCurrentBody();

        // Rush 穿过玩家：Tween 平滑移动到玩家左侧（穿过玩家后离开屏幕左侧）
        if (myNode != null && body != null && targetPos.HasValue)
        {
            var rushTarget = Vector2.Right * (targetPos.Value.X - myNode.GlobalPosition.X - 600f);
            var rushTween = body.CreateTween();
            rushTween.TweenProperty(body, "position", rushTarget, 0.5f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            await Cmd.Wait(0.6f);
        }

        NCombatRoom.Instance?.RadialBlur(VfxPosition.Left);
        await DamageCmd.Attack(FeatherPluckingDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        // 瞬移到右侧，然后 Tween 平滑返回原位
        if (myNode != null && body != null)
        {
            body.Position = Vector2.Right * 600f;
            await Cmd.Wait(0.1f);
            var returnTween = body.CreateTween();
            returnTween.TweenProperty(body, "position", Vector2.Zero, 0.3f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            await Cmd.Wait(0.35f);
        }

        PlayAnim("idle_loop");
    }

    /// <summary>
    /// 绝对输家：给予场上所有单位灾厄，并让玩家失去王国资产。
    /// </summary>
    private async Task AbsoluteLoserMove(IReadOnlyList<Creature> targets)
    {
        PlayAnim("spell");
        await Cmd.Wait(0.5f);

        // 给所有敌人（包括女苑如果还活着）施加灾厄
        var allys = base.CombatState.Allies.Where(c => !c.IsDead).ToList();
        allys.Add(Creature);
        await PowerCmd.Apply<DoomPower>(new ThrowingPlayerChoiceContext(), allys, AbsoluteLoserDoom, base.Creature,
            null);
        foreach (var target in targets)
        {
            var royal = target.GetPowerAmount<RoyaltiesPower>();
            if (royal > 0)
            {
                await PowerCmd.Apply<RoyaltiesPower>(new ThrowingPlayerChoiceContext(), target,
                    -(Math.Min(royal, AbsoluteLoserRoyaltiesLoss)), base.Creature, null);
            }
        }

        await Cmd.Wait(1f);
    }

    /// <summary>
    /// 眩晕状态：播放 die 动画并触发对话。
    /// </summary>
    private Task StunnedMove(IReadOnlyList<Creature> targets)
    {
        //PlayAnim("die");
        TalkCmd.Play(_absoluteLoserLine, base.Creature, VfxColor.Purple, VfxDuration.VeryLong);
        PlayAnim("spell");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 自修复状态：播放 hurt 动画，由 TwinSoulPower.DoReattach 执行治疗并切回。
    /// </summary>
    private Task SelfRepairMove(IReadOnlyList<Creature> targets)
    {
        PlayAnim("damage");
        return Task.CompletedTask;
    }
}