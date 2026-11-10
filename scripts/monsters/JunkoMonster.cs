using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.encounters;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 纯狐：第三幕先古之民的挑战 Boss。
/// 一阶段只有鹤归不休；无心璨炎被破防（首次未格挡伤害 / 生命跌破阈值）后进入二阶段，
/// 下一招强制为宙覆星合，随后按「弹幕地狱 → 至纯洄光 → 弹幕地狱 → 弹幕地狱」循环。
/// 立绘：一阶段 junko2，破防瞬间 junko3，宙覆星合之后固定 junko1。
/// </summary>
public sealed class JunkoMonster : TouhouAncientMonsterBase
{
    protected override bool HasAnimation => false;
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 366, 355);

    private int CeaselessCraneDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);
    private int RevolvingLightDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 35, 30);

    private const int BulletHellDamage = 20;
    private const int RevolvingLightStrength = 10;

    /// <summary>鹤归不休的增力：原「每回合开始 +5 力量」移到此招上。</summary>
    private const int CeaselessCraneStrength = 5;

    /// <summary>无心璨炎的生命值触发阈值（多人由能力自身放大）。</summary>
    private const int HeartlessBlazeThreshold = 200;

    /// <summary>绀珠悯悼初始层数；徽章「完美无缺」据此判断是否触发过复活。</summary>
    internal static int ProtectionLayers => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 4);

    private bool _phaseTwo;

    /// <summary>弹幕地狱段数，每次使用后 +1。</summary>
    private int _bulletHellCombo = 2;

    private MoveState _cosmicConjunction = null!;

    private const string SpriteDir = "res://images/sprite/junko/";
    private const string TransitionSprite = SpriteDir + "junko3.png";
    private const string PhaseTwoSprite = SpriteDir + "junko1.png";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<HeartlessBlazePower>(new ThrowingPlayerChoiceContext(), base.Creature, HeartlessBlazeThreshold, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState ceaselessCrane = new MoveState("CEASELESS_CRANE_RETURN", CeaselessCraneMove,
            new SingleAttackIntent(CeaselessCraneDamage), new BuffIntent());
        // 二阶段被 SetMoveImmediate 强制切换后，必须撑过一次出招，
        // 否则下个玩家回合的 RollMove 会直接跳到 FollowUpState（宙覆星合被跳过）
        _cosmicConjunction = new MoveState("COSMIC_CONJUNCTION", CosmicConjunctionMove, new StatusIntent(1))
        {
            MustPerformOnceBeforeTransitioning = true
        };
        MoveState revolvingLight = new MoveState("PURE_REVOLVING_LIGHT", RevolvingLightMove,
            new SingleAttackIntent(RevolvingLightDamage), new BuffIntent());
        MoveState bulletHellA = new MoveState("PURE_BULLET_HELL", PureBulletHellMove,
            new MultiAttackIntent(BulletHellDamage, () => _bulletHellCombo));
        // 状态名以 2 结尾：图鉴不重复列出同名的弹幕地狱
        MoveState bulletHellB = new MoveState("PURE_BULLET_HELL2", PureBulletHellMove,
            new MultiAttackIntent(BulletHellDamage, () => _bulletHellCombo));

        ceaselessCrane.FollowUpState = ceaselessCrane;
        _cosmicConjunction.FollowUpState = bulletHellA;
        bulletHellA.FollowUpState = revolvingLight;
        revolvingLight.FollowUpState = bulletHellB;
        bulletHellB.FollowUpState = bulletHellA;

        states.Add(ceaselessCrane);
        states.Add(_cosmicConjunction);
        states.Add(revolvingLight);
        states.Add(bulletHellA);
        states.Add(bulletHellB);

        return new MonsterMoveStateMachine(states, ceaselessCrane);
    }

    /// <summary>
    /// 二阶段入口：由 <see cref="HeartlessBlazePower"/> 在破防时调用。
    /// 先播台词并换上 junko3，再强制下一招为宙覆星合（无眩晕）；绀珠之庇由宙覆星合自身施加。
    /// </summary>
    public void EnterPhaseTwo(bool fromUnblockedDamage)
    {
        if (_phaseTwo || base.Creature.IsDead) return;
        _phaseTwo = true;

        SwitchVisual(TransitionSprite);
        string key = $"TOUHOUANCIENTS-JUNKO_MONSTER.moves.SWITCH.banter{(fromUnblockedDamage ? 1 : 2)}";
        TalkCmd.Play(new LocString("monsters", key), base.Creature, VfxColor.Purple, VfxDuration.VeryLong);

        SetMoveImmediate(_cosmicConjunction, forceTransition: true);
    }

    /// <summary>静态立绘换图；显示节点未就绪（图鉴预览、已销毁）时静默跳过。</summary>
    private void SwitchVisual(string path, bool shouldFlip = true)
    {
        if (!IsMutable) return;
        if (base.Creature.GetCreatureNode()?.Visuals.GetCurrentBody() is not Sprite2D sprite) return;

        Texture2D? texture = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        if (texture != null)
        {
            sprite.Texture = texture;
        }

        sprite.FlipH = shouldFlip;
    }

    /// <summary>鹤归不休：单段伤害并叠加力量（增力需在伤害前，否则本次伤害的破防会清掉刚刚获得的力量）。</summary>
    private async Task CeaselessCraneMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, CeaselessCraneStrength, base.Creature, null);
        await DamageCmd.Attack(CeaselessCraneDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>宙覆星合：给所有存活玩家施加绀珠之庇，并各给 1 张带保留的孤注一掷。</summary>
    private async Task CosmicConjunctionMove(IReadOnlyList<Creature> targets)
    {
        VfxCmd.PlayOnCreatureCenter(base.Creature, "vfx/vfx_scream");
        foreach (Creature target in targets.Where(t => !t.IsDead))
        {
            if (target.Player is not { } player) continue;

            TheGambit gambit = player.Creature.CombatState!.CreateCard<TheGambit>(player);
            gambit.AddKeyword(CardKeyword.Retain);
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardsToCombat([gambit], PileType.Hand, null),
                1f);
        }

        await Cmd.Wait(1f);
        await PowerCmd.Apply<UltramarineOrbProtectionPower>(new ThrowingPlayerChoiceContext(), targets.Where(t => !t.IsDead), ProtectionLayers, base.Creature, null);

        // 二阶段 BGM 与曲名在此切换（挑战战斗才用 JunkoEncounter）
        if (base.CombatState?.Encounter is JunkoEncounter encounter)
        {
            encounter.SwitchToPhaseTwoBgm();
        }

        // 破防演出（junko3）到此结束，进入二阶段本体立绘
        SwitchVisual(PhaseTwoSprite, false);
    }

    /// <summary>纯粹的弹幕地狱：按当前连击段数造成多段伤害，出招后段数 +1。</summary>
    private async Task PureBulletHellMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BulletHellDamage)
            .FromMonster(this)
            .WithHitCount(_bulletHellCombo)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        _bulletHellCombo++;
    }

    /// <summary>至纯洄光：造成伤害并获得力量。</summary>
    private async Task RevolvingLightMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(RevolvingLightDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, RevolvingLightStrength, base.Creature, null);
    }
}