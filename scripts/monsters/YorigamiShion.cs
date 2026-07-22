using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

public sealed class YorigamiShion : CustomMonsterModel
{
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
    private int FeatherPluckingRoyaltiesLoss => 10;
    private int AbsoluteLoserDoom => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 18, 16);
    private int AbsoluteLoserRoyaltiesLoss => 10;

    // --- 音效 ---
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

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

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<UnfortunatePower>(base.Creature, 1m, base.Creature, null);
        await PowerCmd.Apply<TwinSoulPower>(base.Creature, 1m, base.Creature, null);
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
        MoveState absoluteLoser = new MoveState("ABSOLUTE_LOSER", AbsoluteLoserMove,
            new DebuffIntent());
        absoluteLoser.FollowUpState = absoluteLoser;

        // 根条件分支：女苑存活 → 阶段1，否则 → 阶段2
        ConditionalBranchState rootBranch = new ConditionalBranchState("ROOT");
        rootBranch.AddState(doomSpread, IsJoonAlive);
        rootBranch.AddState(absoluteLoser);

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
    /// 女苑死亡时：紫苑本回合眩晕，下一回合切换到阶段2。
    /// </summary>
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature == base.Creature) return Task.CompletedTask;
        if (creature.Monster is YorigamiJoon)
        {
            SetMoveImmediate(StunnedState);
        }
        return Task.CompletedTask;
    }

    // --- 技能方法 ---

    /// <summary>
    /// 厄运传播：给予 1 虚弱 + 灾厄。
    /// </summary>
    private async Task DoomSpreadMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<WeakPower>(targets, DoomSpreadWeak, base.Creature, null);
        await PowerCmd.Apply<DoomPower>(targets, DoomSpreadDoom, base.Creature, null);
    }

    /// <summary>
    /// 雁过拔毛：造成伤害。如果实际造成伤害，玩家失去王国资产。
    /// </summary>
    private async Task FeatherPluckingMove(IReadOnlyList<Creature> targets)
    {
        var attackCmd = await DamageCmd.Attack(FeatherPluckingDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        // 如果实际造成了未被格挡的伤害
        if (attackCmd.Results.Any(r => r.UnblockedDamage > 0))
        {
            Player player = base.CombatState.Players[0];
            RoyaltiesPower royalties = player.GetPower<RoyaltiesPower>();
            if (royalties != null && royalties.Amount > 0)
            {
                int lossAmount = (int)System.Math.Min(FeatherPluckingRoyaltiesLoss, royalties.Amount);
                await PowerCmd.Apply<RoyaltiesPower>(player, -lossAmount, base.Creature, null);
            }
        }
    }

    /// <summary>
    /// 绝对输家：给予场上所有单位灾厄，并让玩家失去王国资产。
    /// </summary>
    private async Task AbsoluteLoserMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.8f);

        // 给所有敌人（包括女苑如果还活着）施加灾厄
        IEnumerable<Creature> allEnemies = base.CombatState.Enemies.Where(c => !c.IsDead);
        await PowerCmd.Apply<DoomPower>(allEnemies, AbsoluteLoserDoom, base.Creature, null);

        // 玩家失去王国资产
        Player player = base.CombatState.Players[0];
        RoyaltiesPower royalties = player.GetPower<RoyaltiesPower>();
        if (royalties != null && royalties.Amount > 0)
        {
            int lossAmount = (int)System.Math.Min(AbsoluteLoserRoyaltiesLoss, royalties.Amount);
            await PowerCmd.Apply<RoyaltiesPower>(player, -lossAmount, base.Creature, null);
        }
    }

    /// <summary>
    /// 眩晕状态：什么也不做。
    /// </summary>
    private Task StunnedMove(IReadOnlyList<Creature> targets)
    {
        return Task.CompletedTask;
    }
}
