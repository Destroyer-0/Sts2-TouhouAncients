using System.Collections.Generic;
using System.Text.RegularExpressions;
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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

public sealed class YorigamiJoon : CustomMonsterModel
{
    // --- 本地化 ---
    private static readonly LocString _scatterWealthLine = new LocString("monsters", "TOUHOUANCIENTS-YORIGAMI_JOON.moves.SCATTER_WEALTH_UPPERCUT.banter");
    private static readonly LocString _joonDefeatedLine = new LocString("monsters", "TOUHOUANCIENTS-YORIGAMI_JOON.banter.JOON_DEFEATED");

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

    public override NCreatureVisuals? CreateCustomVisuals() => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/creature_visuals/YorigamiJoon.tscn");
    // --- 死亡后留场景 ---
    //public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature) => false;
    public override bool ShouldFadeAfterDeath => false;
    public override bool ShouldDisappearFromDoom => false;

    // --- 音效 ---
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    
    // --- 死亡前对话 ---
    // 绕过 TalkCmd.Play 的 IsDead 检查（BeforeDeath 时 IsDead 已为 true），直接创建气泡
    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != base.Creature) return;

        bool shionAlive = base.CombatState.Enemies.Any(c => c is { Monster: YorigamiShion, IsDead: false });
        if (shionAlive)
        {
            string formattedText = _joonDefeatedLine.GetFormattedText();
            double duration = Math.Max(3, GetRawCharCount(formattedText) * 0.2);
            NSpeechBubbleVfx? bubble = NSpeechBubbleVfx.Create(formattedText, base.Creature, duration, VfxColor.Purple);
            if (bubble != null)
                NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(bubble);
        }
    }

    // --- 死亡后隐藏意图 ---
    // 由于 TwinSoulPower 阻止了怪物从战斗中移除，导致 AnimHideIntent 不会被自动调用
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != base.Creature) return;
        NCombatRoom.Instance?.GetCreatureNode(creature)?.AnimHideIntent();
    }

    private static int GetRawCharCount(string bbcodeText)
    {
        string text = Regex.Replace(bbcodeText, "\\[/?[^\\]]+\\]", "");
        return text.Replace("\n", "").Replace("\r", "").Replace(" ", "").Length;
    }

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
            new BuffIntent(), new DebuffIntent());

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
        // TODO: 动画 - await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<RoyaltiesPower>(new ThrowingPlayerChoiceContext(), targets, BubbleQueenRoyalties, base.Creature, null);
    }

    /// <summary>
    /// 黄金龙卷风：4x3 多段攻击。结束后给自己叠加讨债人。
    /// </summary>
    private async Task GoldenTornadoMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(GoldenTornadoDamage)
            .FromMonster(this)
            .WithHitCount(GoldenTornadoHits)
            // TODO: 动画 - .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await PowerCmd.Apply<DebtCollectorPower>(new ThrowingPlayerChoiceContext(), base.Creature, 50m, base.Creature, null);
    }

    /// <summary>
    /// 散财上勾拳：基础伤害 + 玩家王国资产层数一半的额外伤害。
    /// 合并为一次攻击判定。同时扣除玩家一半王国资产。
    /// </summary>
    private async Task ScatterWealthUppercutMove(IReadOnlyList<Creature> targets)
    {
        TalkCmd.Play(_scatterWealthLine, base.Creature, VfxColor.Purple, VfxDuration.Long);

        await DamageCmd.Attack(ScatterWealthUppercutDamage)
            .FromMonster(this)
            // TODO: 动画 - .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        // 扣除玩家一半王国资产
        // if (halfRoyalties > 0)
        // {
        //     await PowerCmd.Apply<RoyaltiesPower>(new ThrowingPlayerChoiceContext(), player.Creature, -halfRoyalties, base.Creature, null);
        // }
    }

    /// <summary>
    /// 名流燃烧：获得力量，给玩家施加脆弱。
    /// </summary>
    private async Task CelebrityBurnMove(IReadOnlyList<Creature> targets)
    {
        // TODO: 动画 - await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, CelebrityBurnStrength, base.Creature, null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, CelebrityBurnFrail, base.Creature, null);
    }
}
