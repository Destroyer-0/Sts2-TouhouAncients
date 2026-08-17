using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

public sealed class YorigamiJoonMonster : TouhouAncientMonsterBase
{
    private static readonly StringName RetainedVisualGroup =
        new StringName("touhou_ancients_yorigami_joon_retained_visual");

    // --- 本地化 ---
    private static readonly LocString _scatterWealthLine = new LocString("monsters", "TOUHOUANCIENTS-YORIGAMI_JOON_MONSTER.moves.SCATTER_WEALTH_UPPERCUT.banter");
    private static readonly LocString _joonDefeatedLine = new LocString("monsters", "TOUHOUANCIENTS-YORIGAMI_JOON_MONSTER.banter.JOON_DEFEATED");

    // --- HP ---
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 110, 105);

    // --- 伤害/数值 ---
    private int BubbleQueenRoyalties => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 40, 35);
    private int GoldenTornadoDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 5, 4);
    private int GoldenTornadoHits => 3;
    private int ScatterWealthUppercutDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 13, 12);
    private int CelebrityBurnStrength => 3;
    private int CelebrityBurnFrail => 2;

    public override bool ShouldFadeAfterDeath => true;
    public override bool ShouldDisappearFromDoom => false;
    
    
    protected override string? GetNextAnimation(string finishedAnimation)
    {
        return finishedAnimation == "attack"
            ? CurrentLoopAnimation
            : base.GetNextAnimation(finishedAnimation);
    }
    
    // --- 死亡前对话 ---
    // 绕过 TalkCmd.Play 的 IsDead 检查（BeforeDeath 时 IsDead 已为 true），直接创建气泡
    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != base.Creature) return;

        bool shionAlive = base.CombatState.Enemies.Any(c => c is { Monster: YorigamiShionMonster, IsDead: false });
        if (shionAlive)
        {
            string formattedText = _joonDefeatedLine.GetFormattedText();
            double duration = Math.Max(3, GetRawCharCount(formattedText) * 0.2);
            NSpeechBubbleVfx? bubble = NSpeechBubbleVfx.Create(
                formattedText, base.Creature, duration, VfxColor.Purple);
            if (bubble != null)
                NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(bubble);
        }

        PlayAnimation("die");
    }

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
    {
        if (creature != base.Creature)
            return true;

        return !base.CombatState.Enemies.Any(
            c => c is { Monster: YorigamiShionMonster, IsDead: false });
    }

    // --- 死亡后处理 ---
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != base.Creature) return;

        if (creature.CombatState is not CombatState combatState ||
            !combatState.Enemies.Any(c => c is { Monster: YorigamiShionMonster, IsDead: false }))
            return;

        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode != null)
        {
            creatureNode.AnimHideIntent();
            creatureNode.ToggleIsInteractable(on: false);
            creatureNode.AddToGroup(RetainedVisualGroup);
        }

        // Hook 监听者在执行前已经复制到独立列表，可以安全注销逻辑实体。
        // 保留 CombatState 引用，确保后续死亡 Hook 仍能取得原战斗上下文。
        // 不调用 combatState.RemoveCreature，因为当女苑在玩家回合死亡时，
        // 敌方回合的 PerformMove 仍会尝试移除 creature，导致重复移除报错。
        // 让原版 PerformMove 在检测到 creature 死亡时自然处理移除逻辑即可。
        CombatManager.Instance.RemoveCreature(creature);
    }

    /// <summary>
    /// 紫苑死亡时，让仍留在场上的女苑原始显示节点播放原版死亡风化效果。
    /// </summary>
    internal static Task FadeRetainedVisualAfterShionDeath()
    {
        SceneTree? sceneTree = NCombatRoom.Instance?.GetTree();
        List<NCreature> creatureNodes = sceneTree?
            .GetNodesInGroup(RetainedVisualGroup)
            .OfType<NCreature>()
            .Where(GodotObject.IsInstanceValid)
            .ToList() ?? [];

        return FadeAndRemoveRetainedVisuals(creatureNodes);
    }

    /// <summary>
    /// 让因复活机制而保留的完整怪物节点播放原版死亡风化效果。
    /// </summary>
    internal static Task FadeRetainedCreatureVisual(Creature creature)
    {
        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        return FadeAndRemoveRetainedVisuals(creatureNode == null ? [] : [creatureNode]);
    }

    private static async Task FadeAndRemoveRetainedVisuals(
        List<NCreature> creatureNodes)
    {
        NCombatRoom? combatRoom = NCombatRoom.Instance;
        if (combatRoom == null || creatureNodes.Count == 0)
            return;

        foreach (NCreature creatureNode in creatureNodes)
        {
            creatureNode.RemoveFromGroup(RetainedVisualGroup);
            creatureNode.AnimHideIntent();
        }

        NMonsterDeathVfx? deathVfx = NMonsterDeathVfx.Create(creatureNodes);
        if (deathVfx == null)
        {
            foreach (NCreature creatureNode in creatureNodes)
            {
                combatRoom.RemoveCreatureNode(creatureNode);
                creatureNode.QueueFreeSafely();
            }
            return;
        }

        Node? parent = creatureNodes[0].GetParent();
        if (parent == null)
            return;

        parent.AddChildSafely(deathVfx);
        parent.MoveChild(deathVfx, creatureNodes[0].GetIndex());

        Task fadeTask = PlayDeathVfxAndRemoveNodes(deathVfx, creatureNodes);
        foreach (NCreature creatureNode in creatureNodes)
        {
            creatureNode.DeathAnimationTask = fadeTask;
            combatRoom.RemoveCreatureNode(creatureNode);
        }

        await fadeTask;
    }

    private static async Task PlayDeathVfxAndRemoveNodes(
        NMonsterDeathVfx deathVfx, List<NCreature> creatureNodes)
    {
        await Cmd.Wait(0.25f, ignoreCombatEnd: true);
        await deathVfx.PlayVfx();

        foreach (NCreature creatureNode in creatureNodes)
            creatureNode.QueueFreeSafely();
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
        PlayAnimation("money");
        await Cmd.Wait(0.5f);
        var pos = base.Creature.GetCreatureNode().VfxSpawnPosition;
        VfxCmd.PlayVfx(pos, "vfx/vfx_coin_explosion_regular", NCombatRoom.Instance?.CombatVfxContainer);
        SfxCmd.Play("event:/sfx/ui/gold/gold_2");

        await PowerCmd.Apply<RoyaltiesPower>(new ThrowingPlayerChoiceContext(), targets, BubbleQueenRoyalties, base.Creature, null);
        await Cmd.Wait(1.1f);
        VfxCmd.PlayVfx(pos, "vfx/vfx_coin_explosion_regular", NCombatRoom.Instance?.CombatVfxContainer);
        SfxCmd.Play("event:/sfx/ui/gold/gold_2");
        await Cmd.Wait(0.6f);
        PlayCurrentLoopAnimation();
    }

    /// <summary>
    /// 黄金龙卷风：5x3 多段攻击。结束后给自己叠加讨债人。
    /// </summary>
    private async Task GoldenTornadoMove(IReadOnlyList<Creature> targets)
    {
        PlayAnimation("tornado_1");
        
        NCreature myNode = base.Creature.GetCreatureNode();
        Node2D body = myNode?.Visuals.GetCurrentBody();
        // Rush 穿过玩家：Tween 平滑移动到玩家左侧（穿过玩家后离开屏幕左侧）
        if (myNode != null && body != null)
        {
            var rushTarget = Vector2.Up * (myNode.GlobalPosition.Y + 800f);
            var rushTween = body.CreateTween();
            rushTween.TweenProperty(body, "position", rushTarget, 0.3f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            await Cmd.Wait(0.4f);
        }

        NCombatRoom.Instance?.RadialBlur(VfxPosition.Right);
        PlayAnimation("tornado_2");
        var land = targets.Count > 0 ? NCombatRoom.Instance.GetCreatureNode(targets[0]).GlobalPosition - myNode.GlobalPosition : Vector2.Left * 350;

        var rushTween2 = body.CreateTween();
        rushTween2.TweenProperty(body, "position", land, 0.5f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        await Cmd.Wait(0.5f);
        
        //NCombatRoom.Instance?.RadialBlur(VfxPosition.Left);
        await DamageCmd.Attack(GoldenTornadoDamage)
            .FromMonster(this)
            .WithHitCount(GoldenTornadoHits)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        
        await Cmd.Wait(0.5f);

        PlayAnimation("roll");
        
        Vector2 returnStart = body.Position;
        Vector2 returnEnd = Vector2.Zero;
        float arcHeight = 100f;

        var returnTween = body.CreateTween();
        returnTween.TweenMethod(
            Callable.From<float>(t =>
            {
                body.Position = returnStart.Lerp(returnEnd, t)
                    + new Vector2(0, -10 * arcHeight * t * (1 - t));
            }),
            0f, 1f, 0.5f
        ).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
        await Cmd.Wait(0.5f);
        
        PlayAnimation("prepare");
        await Cmd.Wait(0.25f);
        
        await PowerCmd.Apply<DebtCollectorPower>(new ThrowingPlayerChoiceContext(), base.Creature, 50m, base.Creature, null);
        PlayCurrentLoopAnimation();
    }

    /// <summary>
    /// 散财上勾拳：基础伤害 + 玩家王国资产层数一半的额外伤害。
    /// 合并为一次攻击判定。同时扣除玩家一半王国资产。
    /// </summary>
    private async Task ScatterWealthUppercutMove(IReadOnlyList<Creature> targets)
    {
        await Cmd.Wait(0.4f);
        TalkCmd.Play(_scatterWealthLine, base.Creature, VfxColor.Purple, VfxDuration.VeryLong);

        await Cmd.Wait(0.5f);
        PlayAnimation("attack");
        await Cmd.Wait(0.2f);
        NCombatRoom.Instance?.RadialBlur(VfxPosition.Left);
        await DamageCmd.Attack(ScatterWealthUppercutDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await Cmd.Wait(1f);
        //PlayAnimation("idle");
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
        PlayAnimation("spell_pre");
        await Cmd.Wait(0.2f);
        PlayAnimation("spell");
        // TODO: 动画 - await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, CelebrityBurnStrength, base.Creature, null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, CelebrityBurnFrail, base.Creature, null);
        await Cmd.Wait(0.8f);
        PlayCurrentLoopAnimation();
    }
}
