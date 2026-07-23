using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

public sealed class YorigamiShion : CustomMonsterModel
{
    // --- 本地化 ---
    private static readonly LocString _absoluteLoserLine = new LocString("monsters", "TOUHOUANCIENTS-YORIGAMI_SHION.moves.ABSOLUTE_LOSER.banter");

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
    public override NCreatureVisuals? CreateCustomVisuals() => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/creature_visuals/YorigamiShion.tscn");


    // --- 音效 ---
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    // --- 状态（延迟初始化） ---
    private MoveState _stunnedState;
    private MoveState StunnedState
    {
        get
        {
            _stunnedState ??= new MoveState("STUNNED", StunnedMove);
            return _stunnedState;
        }
    }

    private bool _justExitedStun;
    
    
    public override bool ShouldFadeAfterDeath => false;
    public override bool ShouldDisappearFromDoom => false;


    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<UnfortunatePower>(new ThrowingPlayerChoiceContext(), base.Creature, 5m, base.Creature, null);
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

        // 眩晕结束后进入阶段2
        StunnedState.FollowUpState = absoluteLoser;

        // 根条件分支：女苑存活 → 阶段1，否则 → 阶段2
        ConditionalBranchState rootBranch = new ConditionalBranchState("ROOT");
        rootBranch.AddState(doomSpread, IsJoonAlive);
        rootBranch.AddState(absoluteLoser, () => !IsJoonAlive());

        list.Add(doomSpread);
        list.Add(featherPlucking);
        list.Add(absoluteLoser);
        list.Add(StunnedState);
        list.Add(rootBranch);

        return new MonsterMoveStateMachine(list, rootBranch);
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
            SetMoveImmediate(StunnedState);
            await CreatureCmd.Stun(creature, StunnedMove, StunnedState.StateId);
        }
    }

    // --- 技能方法 ---

    /// <summary>
    /// 厄运传播：给予 1 虚弱 + 灾厄。
    /// </summary>
    private async Task DoomSpreadMove(IReadOnlyList<Creature> targets)
    {
        // TODO: 动画 - await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, DoomSpreadWeak, base.Creature, null);
        await PowerCmd.Apply<DoomPower>(new ThrowingPlayerChoiceContext(), targets, DoomSpreadDoom, base.Creature, null);
    }

    /// <summary>
    /// 雁过拔毛：造成伤害。
    /// </summary>
    private async Task FeatherPluckingMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(FeatherPluckingDamage)
            .FromMonster(this)
            //.WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>
    /// 绝对输家：给予场上所有单位灾厄，并让玩家失去王国资产。
    /// 眩晕后首次进入时触发对话。
    /// </summary>
    private async Task AbsoluteLoserMove(IReadOnlyList<Creature> targets)
    {
        //await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.8f);

        if (_justExitedStun)
        {
            _justExitedStun = false;
            TalkCmd.Play(_absoluteLoserLine, base.Creature, VfxColor.Purple);
        }

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
    }

    /// <summary>
    /// 眩晕状态：什么也不做，标记已退出眩晕。
    /// </summary>
    private Task StunnedMove(IReadOnlyList<Creature> targets)
    {
        _justExitedStun = true;
        return Task.CompletedTask;
    }
}
