using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace TouhouAncients.Scripts.monsters;

public sealed class YorigamiJoon : CustomMonsterModel
{
    // --- HP ---
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 95, 90);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 95, 90);

    // --- 伤害/数值 ---
    private int BubbleQueenRoyalties => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 35, 30);
    private int GoldenTornadoDamage => 4;
    private int GoldenTornadoHits => 3;
    private int ScatterWealthUppercutDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 10, 8);
    private int CelebrityBurnStrength => 2;
    private int CelebrityBurnFrail => 2;

    // --- 音效 ---
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

    // --- 状态机 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();

        MoveState bubbleQueen = new MoveState("BUBBLE_QUEEN", BubbleQueenMove,
            new DebuffIntent());
        MoveState goldenTornado = new MoveState("GOLDEN_TORNADO", GoldenTornadoMove,
            new MultiAttackIntent(GoldenTornadoDamage, GoldenTornadoHits));
        MoveState scatterWealthUppercut = new MoveState("SCATTER_WEALTH_UPPERCUT", ScatterWealthUppercutMove,
            new SingleAttackIntent(ScatterWealthUppercutDamage));
        MoveState celebrityBurn = new MoveState("CELEBRITY_BURN", CelebrityBurnMove,
            new BuffIntent());

        bubbleQueen.FollowUpState = goldenTornado;
        goldenTornado.FollowUpState = scatterWealthUppercut;
        scatterWealthUppercut.FollowUpState = celebrityBurn;
        celebrityBurn.FollowUpState = bubbleQueen;

        list.Add(bubbleQueen);
        list.Add(goldenTornado);
        list.Add(scatterWealthUppercut);
        list.Add(celebrityBurn);

        return new MonsterMoveStateMachine(list, bubbleQueen);
    }

    // --- 技能方法 ---

    /// <summary>
    /// 泡沫的女王：给玩家叠加王国资产。
    /// </summary>
    private async Task BubbleQueenMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<RoyaltiesPower>(targets, BubbleQueenRoyalties, base.Creature, null);
    }

    /// <summary>
    /// 黄金龙卷风：4x3 多段攻击。
    /// </summary>
    private async Task GoldenTornadoMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(GoldenTornadoDamage)
            .FromMonster(this)
            .WithHitCount(GoldenTornadoHits)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>
    /// 散财上勾拳：基础伤害 + 玩家王国资产层数一半的额外伤害。
    /// 合并为一次攻击判定。同时扣除玩家一半王国资产。
    /// </summary>
    private async Task ScatterWealthUppercutMove(IReadOnlyList<Creature> targets)
    {
        Player player = base.CombatState.Players[0];
        RoyaltiesPower royalties = player.GetPower<RoyaltiesPower>();
        int halfRoyalties = 0;
        if (royalties != null && royalties.Amount > 0)
        {
            halfRoyalties = (int)(royalties.Amount / 2m);
        }

        int totalDamage = ScatterWealthUppercutDamage + halfRoyalties;

        await DamageCmd.Attack(totalDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        // 扣除玩家一半王国资产
        if (royalties != null && halfRoyalties > 0)
        {
            await PowerCmd.Apply<RoyaltiesPower>(player, -halfRoyalties, base.Creature, null);
        }
    }

    /// <summary>
    /// 名流燃烧：获得力量，给玩家施加脆弱。
    /// </summary>
    private async Task CelebrityBurnMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<StrengthPower>(base.Creature, CelebrityBurnStrength, base.Creature, null);
        await PowerCmd.Apply<FrailPower>(targets, CelebrityBurnFrail, base.Creature, null);
    }
}
