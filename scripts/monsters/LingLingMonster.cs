using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 铃铃：梅蒂欣·梅兰可莉的随从（位于左侧）。
/// 天生带有幻象（自动附带爪牙）。只使用神经毒素循环：
/// 若梅蒂欣没有毒人偶则补 1 层，然后按毒人偶层数向玩家弃牌堆加入毒素。
/// 铃铃倒下时，梅蒂欣的毒人偶层数减少 1（不会少于 1）。
/// </summary>
public sealed class LingLingMonster : TouhouAncientMonsterBase
{
    protected override bool HasAnimation => false;

    public override bool IsPrimaryMonster => false;

    // 灾厄处决不永久移除，交回幻象复活（参考原版寄生惧魔 / 利齿之眼）
    public override bool ShouldDisappearFromDoom => false;

    // --- HP：括号外为高阶，括号内为低阶 ---
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 13, 12);

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // 幻象会自动附带爪牙（IllusionPower.AfterApplied 中 Apply MinionPower）
        await PowerCmd.Apply<IllusionPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    // --- 状态机：神经毒素自环 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();
        MoveState poison = new MoveState("POISON", PoisonMove, new LingLingPoisonIntent());
        poison.FollowUpState = poison;
        list.Add(poison);
        return new MonsterMoveStateMachine(list, poison);
    }

    // --- 技能方法 ---

    /// <summary>
    /// 神经毒素：若梅蒂欣没有毒人偶则补 1 层，再按毒人偶层数向每个玩家的弃牌堆加入毒素。
    /// </summary>
    private async Task PoisonMove(IReadOnlyList<Creature> targets)
    {
        Creature? medicine = FindMedicine();
        if (medicine != null)
        {
            PoisonDollPower? doll = medicine.GetPower<PoisonDollPower>();
            if (doll == null || doll.Amount <= 0)
            {
                await PowerCmd.Apply<PoisonDollPower>(new ThrowingPlayerChoiceContext(), medicine, 1m, base.Creature, null);
            }
        }

        int dollCount = medicine?.GetPower<PoisonDollPower>()?.Amount ?? 1;
        await CardPileCmd.AddToCombatAndPreview<Toxic>(targets, PileType.Discard, dollCount, null, CardPilePosition.Random);
    }

    // --- 死亡处理 ---

    /// <summary>
    /// 铃铃倒下时：梅蒂欣的毒人偶层数减少 1（不会少于 1）。
    /// 梅蒂欣倒下时：移除幻象让铃铃不再复活，并让保留的贴图正常风化消失。
    /// </summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);

        // 梅蒂欣倒下：铃铃不再复活，后者交回原版死亡流程正常风化移除
        if (creature != base.Creature && creature.Monster is MedicineMelancholyMonster)
        {
            await PowerCmd.Remove<IllusionPower>(base.Creature);

            // 已被幻象保留的尸体不会再走任何死亡流程，手动补风化
            if (base.Creature.IsDead)
            {
                FadeRetainedVisual();
                RemoveFromCombat();
            }

            return;
        }

        if (creature != base.Creature) return;

        Creature? medicine = FindMedicine();
        if (medicine == null) return;

        PoisonDollPower? doll = medicine.GetPower<PoisonDollPower>();
        if (doll == null || doll.Amount <= 1) return;

        await PowerCmd.ModifyAmount(choiceContext, doll, -1m, base.Creature, null);
    }

    /// <summary>把已死且节点已回收的铃铃移出战斗，避免残留实体被后续流程二次处理。</summary>
    private void RemoveFromCombat()
    {
        ICombatState? combatState = base.Creature.CombatState;
        bool performingMove = base.Creature.Monster is { IsPerformingMove: true };
        CombatManager.Instance.RemoveCreature(base.Creature);
        combatState?.RemoveCreature(base.Creature, unattach: performingMove);
    }

    /// <summary>
    /// 在同队中找到梅蒂欣·梅兰可莉。
    /// </summary>
    private Creature? FindMedicine()
    {
        return base.Creature.CombatState?
            .GetTeammatesOf(base.Creature)
            .FirstOrDefault(c => c is { Monster: MedicineMelancholyMonster, IsDead: false });
    }
}
