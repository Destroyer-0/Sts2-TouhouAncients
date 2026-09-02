using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 雾雨魔理沙：偷牌的魔法使。
/// 状态机：逃逸速度 → 蘑菇分支（蘑菇少于上限时召唤，否则进入随机分支）→ 极限火花·蓄力 → 极限火花 → 循环。
/// </summary>
public sealed class KirisameMarisaMonster : TouhouAncientMonsterBase
{
    // --- 动画状态表 ---
    protected override void ConfigureAnimationStateMachine(MonsterAnimationStateMachine animationMachine)
    {
        // 循环：idle（默认）/ dash（飞行）/ roll（空中翻滚）/ jump_rise / jump_fall / shot_2 / spell（蓄力保持）/ masterspark
        // 一次性：dash_start / dash_end / shot_1 / throw
        animationMachine.RegisterLoop("idle");
        animationMachine.RegisterLoop("dash");
        animationMachine.RegisterLoop("roll");
        animationMachine.RegisterLoop("jump_rise");
        animationMachine.RegisterLoop("jump_fall");
        animationMachine.RegisterLoop("shot_2");
        animationMachine.RegisterLoop("spell");
        animationMachine.RegisterLoop("masterspark");
        animationMachine.RegisterOneShot("dash_start");
        animationMachine.RegisterOneShot("dash_end");
        animationMachine.RegisterOneShot("shot_1");
        animationMachine.RegisterOneShot("throw");
    }

    // --- HP ---
    /// <summary>
    /// 初始生命：二层数值（角色最早出现的幕，也作为图鉴预览等环境的回退值）。
    /// 三层时在 <see cref="AfterAddedToRoom"/> 中提升，因为 Creature 构造函数读取
    /// MinInitialHp/MaxInitialHp 时 Creature 尚未绑定，无法获取幕号。
    /// </summary>
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 186, 176);

    /// <summary>三层初始生命（当前数值）。</summary>
    private int InitialHpAct3 => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 286, 270);

    // --- 伤害/数值 ---
    /// <summary>
    /// 逃逸速度基础伤害：默认使用第二幕数值（15/14），第三幕额外配置为 22/20（A17+ 升阶 / 普通）。
    /// 图鉴预览等无法获取幕号的环境同样回退到第二幕数值。
    /// </summary>
    private int EscapeVelocityDamage => GetActValue(
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12),
        (3, AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 17)));

    private int StellarFantasyDamage => GetActValue(
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3),
        (3, AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4)));

    private int StellarFantasyHits => 4;

    private int BlackHoleEdgeDamage => GetActValue(
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 16),
        (3, AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 22, 20)));

    private int BlackHoleEdgeWeak => 2;

    private int MasterSparkChargeBlock => GetActValue(16, (3, 30));

    private int MasterSparkChargeVigorPerMushroom => GetActValue(6, (3, 10));

    private int MasterSparkDamage => GetActValue(30, (3, 40));

    private int MasterSparkStrength => GetActValue(3, (3, 4));

    private const int MaxMushroomCount = 5;

    private const int FungusExpertSummonCount = 2;
    
    private static readonly LocString _mushroomLine =
        new LocString("monsters", "TOUHOUANCIENTS-KIRISAME_MARISA_MONSTER.moves.FUNGUS_EXPERT.banter");
    
    private static readonly LocString _prepareLine =
        new LocString("monsters", "TOUHOUANCIENTS-KIRISAME_MARISA_MONSTER.moves.MASTER_SPARK_CHARGE.banter");
    
    private static readonly LocString _thiefLine =
        new LocString("monsters", "TOUHOUANCIENTS-KIRISAME_MARISA_MONSTER.moves.ESCAPE_VELOCITY.banter");

    public MoveState MasterSparkState;
    
    /// <summary>
    /// 魔理沙身后当前显示的偷取卡牌节点（仅本机创建），归还卡牌时统一清除。
    /// </summary>
    private readonly List<NCard> _stolenCardNodes = new();

    /// <summary>
    /// 场上存活蘑菇的数量。
    /// </summary>
    private int AliveMushroomCount => base.CombatState.Enemies.Count(c => c is { Monster: FantasyMushroomMonster, IsDead: false });

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (CurrentActNumber == 3 )
        {
            SetActInitialHp(InitialHpAct3);
        }
        // 施加量 = 每蘑菇提供的活力值（Counter 类型，层数即活力值）
        await PowerCmd.Apply<MagicianPower>(new ThrowingPlayerChoiceContext(), base.Creature, MasterSparkChargeVigorPerMushroom, base.Creature, null);
    }

    // --- 状态机 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();

        MoveState escapeVelocity = new MoveState("ESCAPE_VELOCITY", EscapeVelocityMove,
            new SingleAttackIntent(EscapeVelocityDamage), new CardDebuffIntent());
        MoveState fungusExpert = new MoveState("FUNGUS_EXPERT", FungusExpertMove, new SummonIntent());
        MoveState stellarFantasy = new MoveState("STELLAR_FANTASY", StellarFantasyMove,
            new MultiAttackIntent(StellarFantasyDamage, StellarFantasyHits));
        MoveState blackHoleEdge = new MoveState("BLACK_HOLE_EDGE", BlackHoleEdgeMove,
            new SingleAttackIntent(BlackHoleEdgeDamage), new DebuffIntent());
        MoveState charge = new MoveState("MASTER_SPARK_CHARGE", MasterSparkChargeMove,
            new DefendIntent(), new BuffIntent());
        MoveState masterSpark = new MoveState("MASTER_SPARK", MasterSparkMove,
            new SingleAttackIntent(MasterSparkDamage), new BuffIntent());

        // 随机分支：星辰幻想 / 黑洞边缘（均不可连续使用）
        // RandomBranchState randomBranch = new RandomBranchState("RAND_BRANCH");
        // randomBranch.AddBranch(stellarFantasy, MoveRepeatType.CannotRepeat);
        // randomBranch.AddBranch(blackHoleEdge, MoveRepeatType.CannotRepeat);

        // 蘑菇分支：蘑菇少于上限时召唤，否则直接路由到随机分支（不空过）
        ConditionalBranchState mushroomBranch = new ConditionalBranchState("MUSHROOM_BRANCH");
        mushroomBranch.AddState(fungusExpert, () => AliveMushroomCount < MaxMushroomCount);
        mushroomBranch.AddState(stellarFantasy, () => AliveMushroomCount >= MaxMushroomCount);

        // 分支后判定：星辰幻想与黑洞边缘都用过一次后，必定进入极限火花·蓄力
        // ConditionalBranchState afterBranch = new ConditionalBranchState("AFTER_BRANCH");
        // afterBranch.AddState(charge, () => _hasUsedStellarFantasy && _hasUsedBlackHoleEdge);
        // afterBranch.AddState(randomBranch, () => !_hasUsedStellarFantasy || !_hasUsedBlackHoleEdge);

        escapeVelocity.FollowUpState = mushroomBranch;
        fungusExpert.FollowUpState = stellarFantasy;
        stellarFantasy.FollowUpState = blackHoleEdge;
        blackHoleEdge.FollowUpState = charge;
        charge.FollowUpState = masterSpark;
        masterSpark.FollowUpState = escapeVelocity;

        list.Add(escapeVelocity);
        list.Add(fungusExpert);
        list.Add(stellarFantasy);
        list.Add(blackHoleEdge);
        list.Add(charge);
        list.Add(masterSpark);
        list.Add(mushroomBranch);
        // list.Add(randomBranch);
        // list.Add(afterBranch);

        return new MonsterMoveStateMachine(list, escapeVelocity);
    }

    // --- 技能方法 ---

    /// <summary>
    /// 逃逸速度：冲刺撞击玩家时偷走每个玩家弃牌堆顶的牌并造成伤害，随后带着被偷的牌从右侧返回。
    /// 弃牌堆为空则不偷，技能正常执行。
    /// </summary>
    private async Task EscapeVelocityMove(IReadOnlyList<Creature> targets)
    {
        NCreature creatureNode = base.Creature.GetCreatureNode();
        Node2D body = creatureNode?.Visuals.GetCurrentBody();
        Vector2 bodyOrigin = body?.Position ?? Vector2.Zero;

        // 找到最左侧玩家，作为撞击点
        Vector2? playerPos = null;
        foreach (Creature target in targets)
        {
            NCreature? targetNode = target.GetCreatureNode();
            if (targetNode != null && (!playerPos.HasValue || playerPos.Value.X > targetNode.GlobalPosition.X))
            {
                playerPos = targetNode.GlobalPosition;
            }
        }

        // 冲刺起手：dash_start（短暂延迟）
        Anim.Trigger("dash_start");
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/thieving_hopper/thieving_hopper_steal");
        await Cmd.Wait(0.25f);

        // 开始飞行：dash（循环）冲向玩家（撞击瞬间偷牌并造成伤害）
        Anim.Trigger("dash");
        if (creatureNode != null)
        {
            // 冲至玩家所在位置（撞击点）
            var impactOffset = playerPos.HasValue
                ? Vector2.Right * (playerPos.Value.X - creatureNode.GlobalPosition.X - 600f)
                : Vector2.Left * 1800;
            await MoveBody((b, tween) => tween.TweenProperty(b, "position", impactOffset, 0.15f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad), 0.2f);
        }

        // 撞击瞬间：偷走每个玩家弃牌堆顶的牌
        List<CardModel> stolenCards = new List<CardModel>();
        foreach (Creature target in targets)
        {
            Player? player = target.Player;
            if (target.IsDead || player == null) continue;

            CardPile discardPile = PileType.Discard.GetPile(player);
            CardModel? cardToSteal = discardPile.Cards.LastOrDefault();
            if (cardToSteal == null) continue;

            await CardPileCmd.RemoveFromCombat(cardToSteal);

            // 创建"我会还给你的"能力实例暂存这张被偷的牌（每名玩家最多 1 张，每个实例 1 张）
            IWillReturnItPower returnPower = (IWillReturnItPower)ModelDb.Power<IWillReturnItPower>().ToMutable();
            // Target 设为被偷牌玩家的 Creature：多人下各实例只对对应玩家可见（参考原版 SwipePower.Steal）
            returnPower.Target = cardToSteal.Owner.Creature;
            returnPower.StolenCard = cardToSteal;
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), returnPower, base.Creature, 1m, base.Creature, null);
            stolenCards.Add(cardToSteal);
        }

        // 撞击瞬间造成伤害
        NCombatRoom.Instance?.RadialBlur(VfxPosition.Left);
        await DamageCmd.Attack(EscapeVelocityDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        // 继续冲出屏幕左侧

        // 屏幕外：让被偷的牌挂在移动的 body 上，随魔理沙从右侧一起飞回
        if (stolenCards.Any(LocalContext.IsMine))
        {
            TalkCmd.Play(_thiefLine, base.Creature, VfxColor.Gold, VfxDuration.VeryLong);
            foreach (CardModel card in stolenCards)
            {
                if (creatureNode == null || body == null || !LocalContext.IsMine(card)) continue;

                Marker2D? stolenCardPos = creatureNode.GetSpecialNode<Marker2D>("%StolenCardPos");
                if (stolenCardPos == null) continue;

                NCard? nCard = NCard.Create(card);
                if (nCard == null) continue;

                // 换算到 body 局部坐标（body 缩放 3、StolenCardPos 缩放 0.3），卡牌跟随 body 移动
                nCard.Position = (stolenCardPos.Position - bodyOrigin) / body.Scale.X
                                 + nCard.Size * (0.3f / body.Scale.X) * 0.5f;
                nCard.Scale = Vector2.One * (0.3f / body.Scale.X);
                body.AddChildSafely(nCard);
                nCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
                _stolenCardNodes.Add(nCard);
            }
        }

        // if (creatureNode != null && body != null)
        // {
        //     var rushTarget = playerPos.HasValue
        //         ? Vector2.Right * (playerPos.Value.X - creatureNode.GlobalPosition.X - 600f)
        //         : Vector2.Left * 1800;
        //     var rushTween2 = body.CreateTween();
        //     rushTween2.TweenProperty(body, "position", rushTarget, 0.2f)
        //         .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        //     await Cmd.Wait(0.25f);
        // }
        
        // 从右侧返回：带着被偷的牌飞回（dash 循环动画贯穿整个飞行）
        SnapBodyPosition(Vector2.Right * 600f);
        await MoveBody((b, tween) => tween.TweenProperty(b, "position", bodyOrigin, 0.25f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad), 0.3f);

        // 飞行结束后短暂延迟
        await Cmd.Wait(0.25f);

        // 落地：dash_end
        Anim.Trigger("dash_end");
        await Cmd.Wait(0.3f);
        Anim.TriggerLoop();
    }

    /// <summary>
    /// 菌类专家：跳起（jump_rise）后在空中翻滚（roll）约 1 秒，期间召唤 2 只奇幻蘑菇（受场上存活蘑菇数量上限限制），再落回原地（jump_fall）。
    /// </summary>
    private async Task FungusExpertMove(IReadOnlyList<Creature> targets)
    {
        TalkCmd.Play(_mushroomLine, base.Creature, VfxColor.White, VfxDuration.Long);
        
        NCreature creatureNode = base.Creature.GetCreatureNode();
        Node2D body = creatureNode?.Visuals.GetCurrentBody();
        Vector2 bodyOrigin = body?.Position ?? Vector2.Zero;

        // 跳起：播放 jump_rise 同时把角色往上升
        Anim.Trigger("jump_rise");
        if (body != null)
        {
            await MoveBody((b, tween) => tween.TweenProperty(b, "position", bodyOrigin + Vector2.Up * 200f, 0.3f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad), 0.35f);
        }

        // 空中翻滚 roll 约 1 秒，期间生成蘑菇召唤
        Anim.Trigger("roll");
        int toSummon = Math.Min(FungusExpertSummonCount, MaxMushroomCount - AliveMushroomCount);
        for (int i = 0; i < toSummon; i++)
        {
            await SummonMushroom();
        }
        await Cmd.Wait(1f);

        // 落地：jump_fall 回到原地
        Anim.Trigger("jump_fall");
        if (body != null)
        {
            await MoveBody((b, tween) => tween.TweenProperty(b, "position", bodyOrigin, 0.3f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad), 0.35f);
        }
        Anim.TriggerLoop();
    }

    /// <summary>
    /// 星辰幻想：先摆出起手式（shot_1），再以射击姿态（shot_2）进行 4 段多段攻击。
    /// </summary>
    private async Task StellarFantasyMove(IReadOnlyList<Creature> targets)
    {
        Anim.Trigger("shot_1");
        await Cmd.Wait(0.4f);
        Anim.Trigger("shot_2");
        await DamageCmd.Attack(StellarFantasyDamage)
            .FromMonster(this)
            .WithHitCount(StellarFantasyHits)
            .WithAttackerFx(null, $"event:/sfx/enemy/enemy_attacks/turret_operator/turret_operator_attack")
            .WithHitFx("vfx/vfx_starry_impact", null, "slash_attack.mp3")
            .Execute(null);
        await Cmd.Wait(0.5f);
        Anim.TriggerLoop();
    }

    /// <summary>
    /// 黑洞边缘：投掷（throw）造成伤害并给玩家施加虚弱。
    /// </summary>
    private async Task BlackHoleEdgeMove(IReadOnlyList<Creature> targets)
    {
        Anim.Trigger("throw");
        await DamageCmd.Attack(BlackHoleEdgeDamage)
            .FromMonster(this)
            //.WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/the_kin_priest/the_kin_priest_soul_grenade")
            .WithWaitBeforeHit(1f, 1f)
            .WithHitVfxNode((Creature t) => NKinPriestGrenadeVfx.Create(t))
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, BlackHoleEdgeWeak, base.Creature, null);
        await Cmd.Wait(0.3f);
        Anim.TriggerLoop();
    }

    /// <summary>
    /// 极限火花·蓄力：播放蓄力动画（spell）并保持到发射，获得格挡，归还偷走的牌到手牌，每个存活蘑菇提供 10 点活力。
    /// </summary>
    private async Task MasterSparkChargeMove(IReadOnlyList<Creature> targets)
    {
        Anim.Trigger("spell");
        await CreatureCmd.GainBlock(base.Creature, MasterSparkChargeBlock, ValueProp.Unpowered, null);
        await ReturnStolenCards();

        TalkCmd.Play(_prepareLine, base.Creature, VfxColor.Gold, VfxDuration.VeryLong);
        int vigorAmount = base.Creature.GetPower<MagicianPower>()?.Amount ?? MasterSparkChargeVigorPerMushroom;
        vigorAmount *= AliveMushroomCount;
        if (vigorAmount > 0)
        {
            await PowerCmd.Apply<VigorPower>(new ThrowingPlayerChoiceContext(), base.Creature, vigorAmount, base.Creature, null);
        }
    }

    /// <summary>
    /// 极限火花：播放发射动画（masterspark），生成极限火花光束素材（scale.y 在 0.5 秒内变化至 1），
    /// 发射瞬间强烈震屏，造成伤害并获得力量；演出结束后光束先淡出再卸载。
    /// </summary>
    private async Task MasterSparkMove(IReadOnlyList<Creature> targets)
    {
        Anim.Trigger("masterspark");

        NGame.Instance?.ScreenShake(ShakeStrength.TooMuch, ShakeDuration.Long);
        // 生成极限火花光束素材，scale.y 在 0.5 秒内从 0 变化至 1
        Node2D? masterspark = null;
        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
        if (creatureNode != null)
        {
            PackedScene? sparkScene = GD.Load<PackedScene>("res://images/sprite/marisa/masterspark.tscn");
            if (sparkScene != null)
            {
                masterspark = sparkScene.Instantiate<Node2D>();
                if (masterspark != null)
                {
                    masterspark.Position = creatureNode.VfxSpawnPosition;
                    masterspark.Scale = new Vector2(8f, 0f);
                    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(masterspark);

                    // 让所有 AnimatedSprite2D 从第 0 帧同时开始播放，保证闪光层与光束层帧同步
                    foreach (AnimatedSprite2D sprite in masterspark.GetChildren().OfType<AnimatedSprite2D>())
                    {
                        sprite.Stop();
                        sprite.Play("default");
                    }

                    var sparkTween = masterspark.CreateTween();
                    sparkTween.TweenProperty(masterspark, "scale:y", 8f, 0.5f)
                        .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
                }
            }
        }

        await Cmd.Wait(0.5f);

        // 发射瞬间强烈震屏

        await DamageCmd.Attack(MasterSparkDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await Cmd.Wait(1f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, MasterSparkStrength, base.Creature, null);

        await Cmd.Wait(1f);

        // 演出结束后先淡出再卸载
        if (masterspark != null && GodotObject.IsInstanceValid(masterspark))
        {
            var fadeTween = masterspark.CreateTween();
            fadeTween.TweenProperty(masterspark, "modulate:a", 0f, 0.4f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            await Cmd.Wait(0.5f);
            masterspark.QueueFreeSafely();
        }
        Anim.TriggerLoop();
    }

    /// <summary>
    /// 在空槽位召唤一只奇幻蘑菇，并附加仆从与菌类能力。
    /// </summary>
    private async Task SummonMushroom()
    {
        string? slot = base.CombatState.Encounter?.Slots.LastOrDefault(
            s => base.CombatState.Enemies.All(c => c.SlotName != s));
        if (slot == null) return;

        Creature mushroom = await CreatureCmd.Add(ModelDb.Monster<FantasyMushroomMonster>().ToMutable(), base.CombatState, CombatSide.Enemy, slot);
    }

    /// <summary>
    /// 将魔理沙偷走的牌归还到对应玩家手牌。
    /// </summary>
    private async Task ReturnStolenCards()
    {
        // 清除魔理沙身后显示的偷取卡牌节点：归还后不再展示，直到下次偷取卡牌时重新创建
        foreach (NCard stolenCardNode in _stolenCardNodes)
        {
            stolenCardNode.QueueFreeSafely();
        }
        _stolenCardNodes.Clear();

        // 获取魔理沙身上所有"我会还给你的"能力实例（每个实例暂存一名玩家被偷的 1 张牌）
        List<IWillReturnItPower> returnPowers = base.Creature.GetPowerInstances<IWillReturnItPower>().ToList();
        if (returnPowers.Count == 0) return;

        // 归还每张被偷的牌到对应玩家手牌
        foreach (IWillReturnItPower returnPower in returnPowers)
        {
            CardModel? card = returnPower.StolenCard;
            if (card == null) continue;
            if (card.Owner == null || card.Owner.Creature == null || card.Owner.Creature.IsDead) continue;

            // 牌被移出战斗后需要先恢复其战斗状态标记，才能重新加入手牌
            card.HasBeenRemovedFromState = false;
            await CardPileCmd.Add(card, PileType.Hand);
        }

        // 归还后移除这些能力实例
        foreach (IWillReturnItPower returnPower in returnPowers)
        {
            await PowerCmd.Remove(returnPower);
        }
    }
}
