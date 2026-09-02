using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
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
using TouhouAncients.Scripts.Vfx;

namespace TouhouAncients.Scripts.monsters;

public sealed class YorigamiShionMonster : TouhouAncientMonsterBase
{
    protected override bool ShouldKeepAnimatingWhileDead => Creature.HasPower<TwinSoulPower>();

    // --- 动画状态表 ---
    protected override void ConfigureAnimationStateMachine(MonsterAnimationStateMachine animationMachine)
    {
        // 循环归属：女苑存活 → idle，否则 → spell（阶段2施法姿态）
        animationMachine.LoopResolver = () => IsJoonAlive() ? "idle" : "spell";
        // 眩晕期间禁止受击打断（全局默认，个别状态可显式覆盖）
        animationMachine.DefaultCanBeInterruptedByHit = () => NextMove != StunnedState;

        // 循环：idle / spell / rush
        animationMachine.RegisterLoop("idle");
        animationMachine.RegisterLoop("spell");
        animationMachine.RegisterLoop("rush");
        // 一次性：damage；die 进入时播放 AnimationPlayer 死亡浮空，退出时复位
        animationMachine.RegisterOneShot("damage");
        animationMachine.RegisterOneShot("die",
            onEnter: () => AnimationPlayer?.Play("float"),
            onExit: () => AnimationPlayer?.Play("RESET"));
    }

    // --- 本地化 ---
    private static readonly LocString _absoluteLoserLine =
        new LocString("monsters", "TOUHOUANCIENTS-YORIGAMI_SHION_MONSTER.moves.ABSOLUTE_LOSER.banter");

    // --- HP ---
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 120, 110);

    // --- 伤害/数值 ---
    private int DoomSpreadDoom => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 5, 4);

    private int DoomSpreadWeak => 1;

    private int FeatherPluckingDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 8, 7);

    private int AbsoluteLoserDoom => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 18, 16);

    private int TwinSoulRecoverHP => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 60, 55);

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
        Anim.TriggerLoop();
    }

    public override bool ShouldFadeAfterDeath => true; //IsJoonAlive();
    public override bool ShouldDisappearFromDoom => IsJoonAlive();


    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<UnfortunatePower>(new ThrowingPlayerChoiceContext(), base.Creature, 5m, base.Creature,
            null);
        var twinSoulPower = await PowerCmd.Apply<TwinSoulPower>(new ThrowingPlayerChoiceContext(), base.Creature, 8m, base.Creature, null);
        if (twinSoulPower == null) return;
        var amount = CombatState.Players.Count == 1 ? TwinSoulRecoverHP : twinSoulPower.GetScaledAmountForMultiplayer(CombatState, Creature, TwinSoulRecoverHP, Creature, null);
        twinSoulPower.SetRecoverHp(amount);
    }

    // --- 状态机 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();

        // 阶段1：厄运传播 → 雁过拔毛（循环）
        MoveState doomSpread = new MoveState("DOOM_SPREAD", DoomSpreadMove, new DebuffIntent());
        MoveState featherPlucking = new MoveState("FEATHER_PLUCKING", FeatherPluckingMove, new SingleAttackIntent(FeatherPluckingDamage));

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


    /// <summary>
    /// 检测女苑是否存活。
    /// </summary>
    private bool IsJoonAlive()
    {
        return !CombatManager.Instance.IsInProgress || base.CombatState.Enemies.Any(c => c is { Monster: YorigamiJoonMonster, IsDead: false });
    }

    /// <summary>
    /// 女苑死亡时：紫苑移除双生能力，本回合眩晕，下一回合切换到阶段2。
    /// 紫苑自身死亡时由 TwinSoulPower 处理复活。
    /// </summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature == base.Creature)
        {
            if (!base.Creature.HasPower<TwinSoulPower>())
            {
                Anim.Trigger("die");
                await YorigamiJoonMonster.FadeRetainedVisualAfterShionDeath();
            }

            return;
        }

        if (creature.Monster is YorigamiJoonMonster)
        {
            bool isWaitingToRevive = base.Creature.IsDead;
            await PowerCmd.Remove<TwinSoulPower>(base.Creature);

            Anim.Trigger("die");

            if (isWaitingToRevive)
            {
                await YorigamiJoonMonster.FadeRetainedCreatureVisual(base.Creature);
                return;
            }

            SetMoveImmediate(StunnedState, forceTransition: true);
        }
    }

    public void SetSelfRepairState()
    {
        Anim.Trigger("damage");
        SetMoveImmediate(SelfRepairState);
    }


    private AnimationPlayer? _animationPlayer;

    private AnimationPlayer? AnimationPlayer
    {
        get
        {
            if (_animationPlayer == null)
            {
                _animationPlayer = MyAnimatedSprite2D?.GetParent()?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            }

            return _animationPlayer;
        }
    }

    /// <summary>
    /// 厄运传播：给予 1 虚弱 + 灾厄。
    /// </summary>
    private async Task DoomSpreadMove(IReadOnlyList<Creature> targets)
    {
        Anim.Trigger("spell");
        await Cmd.Wait(0.5f);
        ScatterNegativeGlyphs(targets, count: 10, scatterRadius: 200f);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, DoomSpreadWeak, base.Creature,
            null);
        await PowerCmd.Apply<DoomPower>(new ThrowingPlayerChoiceContext(), targets, DoomSpreadDoom, base.Creature,
            null);
        await Cmd.Wait(0.75f);
        Anim.TriggerLoop();
    }

    /// <summary>
    /// 雁过拔毛：Rush 穿过玩家，离开屏幕左侧后从右侧返回，造成伤害。
    /// </summary>
    private async Task FeatherPluckingMove(IReadOnlyList<Creature> targets)
    {
        Anim.Trigger("rush");
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

        NCreature? myNode = base.Creature.GetCreatureNode();

        // Rush 穿过玩家：Tween 平滑移动到玩家左侧（穿过玩家后离开屏幕左侧）
        if (myNode != null)
        {
            var rushTarget = targetPos.HasValue
                ? Vector2.Right * (targetPos.Value.X - myNode.GlobalPosition.X - 600f)
                : Vector2.Left * 1800;
            await MoveBody((b, tween) => tween.TweenProperty(b, "position", rushTarget, 0.3f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad), 0.4f);
        }

        NCombatRoom.Instance?.RadialBlur(VfxPosition.Left);
        await DamageCmd.Attack(FeatherPluckingDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        ScatterNegativeGlyphs(targets, count: 8, scatterRadius: 180f);

        // 瞬移到右侧，然后 Tween 平滑返回原位
        SnapBodyPosition(Vector2.Right * 600f);
        await MoveBody((b, tween) => tween.TweenProperty(b, "position", Vector2.Zero, 0.25f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad), 0.3f);

        Anim.TriggerLoop();
    }

    /// <summary>
    /// 绝对输家：给予场上所有单位灾厄。
    /// </summary>
    private async Task AbsoluteLoserMove(IReadOnlyList<Creature> targets)
    {
        Anim.Trigger("spell");
        // 给所有敌人（包括女苑如果还活着）施加灾厄
        var allys = base.CombatState.Allies.Where(c => !c.IsDead).ToList();
        allys.Add(Creature);
        SfxCmd.Play("event:/sfx/characters/attack_fire");
        foreach (var target in allys)
        {
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(Creature, VfxColor.Blue));
        }
        ScatterNegativeGlyphs(targets, count: 18, scatterRadius: 280f);
        await Cmd.Wait(0.25f);
        ScatterNegativeGlyphs(targets, count: 18, scatterRadius: 340f);
        await Cmd.Wait(0.25f);

        await PowerCmd.Apply<DoomPower>(new ThrowingPlayerChoiceContext(), allys, AbsoluteLoserDoom, base.Creature, null);

        await Cmd.Wait(1f);
    }

    /// <summary>
    /// 眩晕状态：播放 die 动画并触发对话。
    /// </summary>
    private Task StunnedMove(IReadOnlyList<Creature> targets)
    {
        TalkCmd.Play(_absoluteLoserLine, base.Creature, VfxColor.Purple, VfxDuration.VeryLong);
        VfxCmd.PlayOnCreatureCenter(Creature, "vfx/vfx_scream");
        float scale = 2f;
        NGroundFireVfx nGroundFireVfx = NGroundFireVfx.Create(Creature,VfxColor.Blue);
        if (nGroundFireVfx != null)
        {
            SfxCmd.Play("event:/sfx/characters/attack_fire");
            nGroundFireVfx.Scale = Vector2.One * scale;
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nGroundFireVfx);
        }
        Anim.Trigger("spell");
        return Task.CompletedTask;
    }

    /// <summary>
    /// </summary>
    private Task SelfRepairMove(IReadOnlyList<Creature> targets)
    {
        Anim.Trigger("damage");
        return Task.CompletedTask;
    }

    protected override bool ShouldShowMoveInBestiary(string moveStateId)
    {
        return moveStateId != "STUNNED";
    }

    /// <summary>
    /// 在目标位置逸散随机厄运字符（厄 / 貧 / 損 / 負）。
    /// </summary>
    private void ScatterNegativeGlyphs(IReadOnlyList<Creature> targets, int count, float scatterRadius)
    {
        foreach (Creature target in targets)
        {
            NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(target);
            if (creatureNode == null)
                continue;

            ScatterNegativeGlyphsAt(creatureNode.VfxSpawnPosition, count, scatterRadius);
        }
    }

    private void ScatterNegativeGlyphsAt(Vector2 origin, int count, float scatterRadius)
    {
        if (NCombatRoom.Instance?.CombatVfxContainer == null)
            return;

        NShionNegativeBurstVfx? vfx = NShionNegativeBurstVfx.Create(origin, count, scatterRadius);
        if (vfx == null)
            return;

        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
    }
}