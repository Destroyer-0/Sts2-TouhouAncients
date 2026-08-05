using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 奇幻蘑菇：魔理沙召唤的仆从。每回合恢复生命，死亡时向玩家弃牌堆加入孢子心灵。
/// </summary>
public sealed class FantasyMushroom : TouhouAncientMonster
{
    // --- HP ---
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 39, 35);

    // --- 数值 ---
    private int HealAmount => 10;

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<FungalPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    // --- 死亡处理 ---
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != base.Creature) return;

        // 每个玩家的弃牌堆随机位置加入 3 张孢子心灵
        foreach (Player player in base.CombatState.Players)
        {
            await CardPileCmd.AddToCombatAndPreview<SporeMind>(
                player.Creature, PileType.Discard, 3, player, CardPilePosition.Random);
        }
    }

    // --- 状态机：固定恢复意图（自环） ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();
        MoveState heal = new MoveState("HEAL", HealMove, new HealIntent());
        heal.FollowUpState = heal;
        list.Add(heal);
        return new MonsterMoveStateMachine(list, heal);
    }

    /// <summary>
    /// 恢复：恢复自身生命。
    /// </summary>
    private async Task HealMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.Heal(base.Creature, HealAmount);
    }
}
