using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
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

public sealed class YorigamiJoon : TouhouAncientMonster
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
    public override bool ShouldFadeAfterDeath => IsShionAlive();
    public override bool ShouldDisappearFromDoom => IsShionAlive();

    // --- 音效 ---
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;
    
    /// <summary>
    /// 检测女苑是否存活。
    /// </summary>
    private bool IsShionAlive()
    {
        return base.CombatState.Enemies.Any(c => c.Monster is YorigamiShion && !c.IsDead);
    }

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature) => creature != Creature || !IsShionAlive();
    
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        MyAnimatedSprite2D.AnimationFinished += OnAnimationFinished;
    }
    
    private void OnAnimationFinished()
    {
        if (MyAnimatedSprite2D.Animation == "hurt")
        {
            MyAnimatedSprite2D.Play("idle");
        }
    }
    
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

    // --- 死亡后处理 ---
    // 强制切换到无限眩晕状态，显示眩晕意图。
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != base.Creature) return;
        PlayAnimation("die");
        ForceDeadState();
    }

    /// <summary>
    /// 将状态机强制切换到一个自循环的眩晕状态，死后不再行动。
    /// </summary>
    private void ForceDeadState()
    {
        MonsterMoveStateMachine? fsm = base.Creature.Monster?.MoveStateMachine;
        if (fsm == null) return;
        MoveState deadState = new MoveState("DEAD", DeadMove, new StunIntent())
        {
            FollowUpStateId = "DEAD",
            MustPerformOnceBeforeTransitioning = true,
        };
        fsm.States["DEAD"] = deadState;
        base.Creature.Monster.SetMoveImmediate(deadState, forceTransition: true);
    }

    private static Task DeadMove(IReadOnlyList<Creature> targets)
    {
        return Task.CompletedTask;
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
        var pos = NCombatRoom.Instance.GetCreatureNode(Creature)?.VfxSpawnPosition;
        if (pos.HasValue)
        {
            VfxCmd.PlayVfx(pos.Value, "vfx/vfx_coin_explosion_regular", NCombatRoom.Instance?.CombatVfxContainer);
            SfxCmd.Play("event:/sfx/ui/gold/gold_2");
        }
        await PowerCmd.Apply<RoyaltiesPower>(new ThrowingPlayerChoiceContext(), targets, BubbleQueenRoyalties, base.Creature, null);
        await Cmd.Wait(1.1f);
        if (pos.HasValue)
        {
            VfxCmd.PlayVfx(pos.Value, "vfx/vfx_coin_explosion_regular", NCombatRoom.Instance?.CombatVfxContainer);
            SfxCmd.Play("event:/sfx/ui/gold/gold_2");
        }
        await Cmd.Wait(0.6f);
        PlayAnimation("idle");
    }

    /// <summary>
    /// 黄金龙卷风：4x3 多段攻击。结束后给自己叠加讨债人。
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
        var land = NCombatRoom.Instance.GetCreatureNode(targets[0]).GlobalPosition - myNode.GlobalPosition;

        var rushTween2 = body.CreateTween();
        rushTween2.TweenProperty(body, "position", land, 0.5f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        await Cmd.Wait(0.5f);
        
        //NCombatRoom.Instance?.RadialBlur(VfxPosition.Left);
        await DamageCmd.Attack(GoldenTornadoDamage)
            .FromMonster(this)
            .WithHitCount(GoldenTornadoHits)
            .WithAttackerFx(null, AttackSfx)
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
        PlayAnimation("idle");
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
        MyAnimatedSprite2D.AnimationFinished += Reset;
        await Cmd.Wait(0.2f);
        NCombatRoom.Instance?.RadialBlur(VfxPosition.Left);
        await DamageCmd.Attack(ScatterWealthUppercutDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
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
    
    private void Reset()
    {
        PlayAnimation("idle");
        MyAnimatedSprite2D.AnimationFinished -= Reset;
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
        PlayAnimation("idle");
    }
}
