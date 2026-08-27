using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 饕餮尤魔：先古之民挑战 Boss（第 3 幕，300 万 HP）。
/// 核心机制：玩家通过"欲壑血海"给尤魔喂力量，配合尤魔的"血煞祸劫"使受到的
/// 伤害/失去生命/灾厄层数成指数增长（×2^力量），才能击杀 300 万 HP。
/// 若连续 3 回合不给尤魔喂牌，则触发"混沌炼狱"惩罚（连击次数无限增长）。
///
/// 状态机：
/// 第一回合：无尽贪妄（仅 1 次）→ 剖心撕肉 → 剔骨啖髓 → 饕餮盛宴
/// → [判定] 尤魔连续 3 回合未成为"欲壑血海"目标 → 混沌炼狱（回判定）→ 否则回到剖心撕肉。
/// </summary>
public sealed class ToutetsuYuumaMonster : TouhouAncientMonsterBase
{
    // --- HP：三百万（无进阶区分） ---
    protected override int InitialHp => 3_000_000;

    // --- 数值 ---
    /// <summary>剖心撕肉：每段伤害，低阶 4 / 高阶 5，共 3 段。</summary>
    private int CutHeartDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 4, 3);

    private const int CutHeartHits = 3;

    /// <summary>剔骨啖髓：基础伤害，低阶 16 / 高阶 17。</summary>
    private int DrinkBloodDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 17, 16);

    private const int DrinkBloodVulnerable = 2;

    /// <summary>饕餮盛宴：给予所有玩家的饥渴本能层数。</summary>
    private const int FeastHungryInstinct = 2;

    /// <summary>混沌炼狱：基础伤害（× 连击次数），低阶 20 / 高阶 22。</summary>
    private int ChaosHellBaseDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 12, 11);

    /// <summary>混沌炼狱：每次使用的力量。</summary>
    private const int ChaosHellStrength = 2;

    /// <summary>无尽贪妄：向每个玩家抽/弃牌堆各加入的欲壑血海数量。</summary>
    private const int GreedCardsPerPile = 3;

    /// <summary>触发混沌炼狱所需的连续未喂牌回合数。</summary>
    private const int TurnsWithoutFeedThreshold = 3;

    /// <summary>混沌炼狱连击次数（从 1 开始，每次使用 +1）。</summary>
    private int _chaosHellCombo = 1;

    /// <summary>连续未成为"欲壑血海"目标的回合数。</summary>
    private int _turnsWithoutFeed;

    /// <summary>本回合是否已被"欲壑血海"瞄准过（回合结算后清零）。</summary>
    private bool _targetedThisTurn;
    
    // --- 状态机 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();

        MoveState greed = new MoveState("GREED", GreedMove, new BuffIntent(), new CardDebuffIntent());
        MoveState cutHeart = new MoveState("CUT_HERAT", CutHeartMove,
            new MultiAttackIntent(CutHeartDamage, CutHeartHits));
        MoveState drinkBlood = new MoveState("DRINK_BLOOD", DrinkBloodMove,
            new SingleAttackIntent(DrinkBloodDamage), new DebuffIntent());
        MoveState feast = new MoveState("TOUTETSU_FEAST", FeastMove, new DebuffIntent());

        MoveState chaosHell = new MoveState("CHAOS_HELL", ChaosHellMove,
            new MultiAttackIntent(ChaosHellBaseDamage, () => _chaosHellCombo), new BuffIntent());

        // 判定分支：连续 3 回合未被喂牌 → 混沌炼狱；否则 → 剖心撕肉
        ConditionalBranchState afterFeast = new ConditionalBranchState("AFTER_FEAST");
        afterFeast.AddState(chaosHell, () => _turnsWithoutFeed >= TurnsWithoutFeedThreshold);
        afterFeast.AddState(cutHeart, () => _turnsWithoutFeed < TurnsWithoutFeedThreshold); // 默认：回到剖心撕肉

        greed.FollowUpState = cutHeart;
        cutHeart.FollowUpState = drinkBlood;
        drinkBlood.FollowUpState = feast;
        feast.FollowUpState = afterFeast;
        chaosHell.FollowUpState = afterFeast;

        list.Add(greed);
        list.Add(cutHeart);
        list.Add(drinkBlood);
        list.Add(feast);
        list.Add(chaosHell);
        list.Add(afterFeast);

        return new MonsterMoveStateMachine(list, greed);
    }

    // --- 技能方法 ---

    /// <summary>
    /// 无尽贪妄（仅第一回合）：给予自身 1 层血煞祸劫（不可叠加），
    /// 并向每个玩家的抽牌堆、弃牌堆各加入 3 张"欲壑血海"。
    /// </summary>
    private async Task GreedMove(IReadOnlyList<Creature> targets)
    {
        var choiceContext = new ThrowingPlayerChoiceContext();

        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/the_insatiable/the_insatiable_liquify_ground");
        VfxCmd.PlayOnCreatureCenter(base.Creature, "vfx/vfx_scream");
        await Cmd.Wait(0.75f);

        // 向每个玩家的抽牌堆、弃牌堆各加入 3 张欲壑血海（参照原版无厌沙虫）
        foreach (Player player in base.CombatState.Players)
        {
            var statusCards = new List<CardPileAddResult>();
            for (int i = 0; i < GreedCardsPerPile * 2; i++)
            {
                CardModel card = base.CombatState.CreateCard<DesireBloodSea>(player);
                PileType pileType = i < GreedCardsPerPile ? PileType.Draw : PileType.Discard;
                statusCards.Add(await CardPileCmd.AddGeneratedCardToCombat(card, pileType, null, CardPilePosition.Random));
            }
            if (LocalContext.IsMe(player))
            {
                CardCmd.PreviewCardPileAdd(statusCards);
                await Cmd.Wait(1f);
            }
        }
        
        await Cmd.Wait(0.5f);
        // 自身 1 层血煞祸劫（Single 类型：已有则不再叠加）
        await PowerCmd.Apply<BloodDisasterPower>(choiceContext, base.Creature, 1m, base.Creature, null);
    }

    /// <summary>
    /// 剖心撕肉：3 段攻击。
    /// </summary>
    private async Task CutHeartMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(CutHeartDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .WithHitCount(CutHeartHits)
            .Execute(null);
    }

    /// <summary>
    /// 剔骨啖髓：造成伤害并给予 2 易伤。
    /// </summary>
    private async Task DrinkBloodMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(DrinkBloodDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, DrinkBloodVulnerable, base.Creature, null);
    }

    /// <summary>
    /// 饕餮盛宴：给予所有玩家 2 层饥渴本能。
    /// </summary>
    private async Task FeastMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<HungryInstinctPower>(new ThrowingPlayerChoiceContext(), targets, FeastHungryInstinct, base.Creature, null);
    }

    /// <summary>
    /// 混沌炼狱：造成（基础 × 连击次数）伤害，连击次数 +1，获得 2 力量。
    /// </summary>
    private async Task ChaosHellMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ChaosHellBaseDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitCount(_chaosHellCombo)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, ChaosHellStrength, base.Creature, null);

        // 连击次数 +1
        _chaosHellCombo++;
    }

    /// <summary>
    /// 敌人回合开始时结算"连续未喂牌"计数：
    /// 本回合未被欲壑血海瞄准过 → +1；被瞄准过 → 清零。
    /// 同时重置本回合瞄准标记。
    /// </summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.AfterSideTurnStart(side, participants, combatState);
        if (side != CombatSide.Enemy) return;

        _turnsWithoutFeed = _targetedThisTurn ? 0 : _turnsWithoutFeed + 1;
        _targetedThisTurn = false;
    }

    /// <summary>
    /// 记录尤魔本回合被"欲壑血海"瞄准过（由欲壑血海打出时调用）。
    /// </summary>
    public void NotifyTargetedByDesireBloodSea()
    {
        _targetedThisTurn = true;
    }
}
