using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 梅蒂欣·梅兰可莉：小小的孤独之药（先古之民挑战 Boss）。
/// 技能链式循环：皇后的甜蜜苹果 → 夜莺与玫瑰 → 光荣的荆棘路 → 我可爱的铃铃 → 循环。
/// 天生带有荆棘，并通过毒人偶与烈毒护身与随从铃铃联动。
/// </summary>
public sealed class MedicineMelancholyMonster : TouhouAncientMonsterBase
{
    // --- HP：括号外为高阶，括号内为低阶 ---
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 183, 173);

    // --- 数值 ---
    /// <summary>夜莺与玫瑰：每段伤害，低阶 6 / 高阶 7。</summary>
    private int NightingaleRoseDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 7, 6);

    /// <summary>光荣的荆棘路：基础伤害，低阶 12 / 高阶 13。</summary>
    private int GloryThornyPathDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 13, 12);

    private const int SnowAppleWeak = 2;

    private const int SnowAppleVulnerable = 2;

    private const int GloryThornyThorns = 1;

    private const int LingLingStrength = 2;

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // 初始荆棘 1
        await PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), base.Creature, GloryThornyThorns, base.Creature, null);
        // 初始毒人偶 1（铃铃天生带有毒人偶 1）
        await PowerCmd.Apply<PoisonDollPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    // --- 状态机：四技能依次循环 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();

        MoveState whiteSnow = new MoveState("WHITE_SNOW", SnowAppleMove, new DebuffIntent());
        MoveState nightingaleRose = new MoveState("NIGHTINGALE_ROSE", NightingaleRoseMove,
            new MultiAttackIntent(NightingaleRoseDamage, 2));
        MoveState gloryThornyPath = new MoveState("GLORY_THRONY_PATH", GloryThornyPathMove,
            new SingleAttackIntent(GloryThornyPathDamage), new BuffIntent());
        MoveState ohMyCuteLingLing = new MoveState("OH_MY_CUTE_LINGLING", OhMyCuteLingLingMove,
            new BuffIntent());

        whiteSnow.FollowUpState = nightingaleRose;
        nightingaleRose.FollowUpState = gloryThornyPath;
        gloryThornyPath.FollowUpState = ohMyCuteLingLing;
        ohMyCuteLingLing.FollowUpState = whiteSnow;

        list.Add(whiteSnow);
        list.Add(nightingaleRose);
        list.Add(gloryThornyPath);
        list.Add(ohMyCuteLingLing);

        return new MonsterMoveStateMachine(list, whiteSnow);
    }

    // --- 技能方法 ---

    /// <summary>
    /// 皇后的甜蜜苹果：给予玩家 2 虚弱与 2 易伤。
    /// </summary>
    private async Task SnowAppleMove(IReadOnlyList<Creature> targets)
    {
        var choiceContext = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<WeakPower>(choiceContext, targets, SnowAppleWeak, base.Creature, null);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, targets, SnowAppleVulnerable, base.Creature, null);
    }

    /// <summary>
    /// 夜莺与玫瑰：2 段攻击。
    /// </summary>
    private async Task NightingaleRoseMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(NightingaleRoseDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .WithHitCount(2)
            .Execute(null);
    }

    /// <summary>
    /// 光荣的荆棘路：造成伤害并给自身叠加 1 荆棘。
    /// </summary>
    private async Task GloryThornyPathMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(GloryThornyPathDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), base.Creature, GloryThornyThorns, base.Creature, null);
    }

    /// <summary>
    /// 我可爱的铃铃：自身获得 2 力量；梅蒂欣获得 1 层毒人偶；铃铃获得 1 回合烈毒护身。
    /// </summary>
    private async Task OhMyCuteLingLingMove(IReadOnlyList<Creature> targets)
    {
        var choiceContext = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(choiceContext, base.Creature, LingLingStrength, base.Creature, null);
        await PowerCmd.Apply<PoisonDollPower>(choiceContext, base.Creature, 1m, base.Creature, null);

        Creature? lingLing = base.Creature.CombatState?
            .GetTeammatesOf(base.Creature)
            .FirstOrDefault(c => c is { Monster: LingLingMonster, IsDead: false });
        if (lingLing != null)
        {
            await PowerCmd.Apply<GuardingPower>(choiceContext, lingLing, 1m, base.Creature, null);
        }
    }
}
