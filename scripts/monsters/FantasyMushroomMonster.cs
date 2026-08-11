using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 奇幻蘑菇：魔理沙召唤的仆从。每回合恢复生命，死亡时向玩家弃牌堆加入孢子心灵。
/// 外观有两种随机形态（大蘑菇 / 小蘑菇），视觉锚点位于贴图底部（在场景中烘焙）。
/// </summary>
public sealed class FantasyMushroomMonster : TouhouAncientMonsterBase
{
    // --- 外观 ---
    /// <summary>
    /// 纯静态贴图（Sprite2D），无帧动画，跳过 AnimatedSprite2D 相关处理。
    /// </summary>
    protected override bool HasAnimation => false;

    /// <summary>
    /// 备用小蘑菇场景路径（每个场景各自烘焙底部锚点）。
    /// </summary>
    private const string AlternateMushroomScenePath = "res://scenes/creature_visuals/FantasyMushroomAlt.tscn";

    // --- HP ---
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 49, 45);

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Plant;
    
    // --- 数值 ---
    private int HealAmount => 7;

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<MinionPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
        await PowerCmd.Apply<FungalPower>(new ThrowingPlayerChoiceContext(), base.Creature, 3m, base.Creature, null);
    }

    // --- 视觉 ---
    public override NCreatureVisuals? CreateCustomVisuals()
    {
        // 随机二选一：备用小蘑菇场景 / 默认大蘑菇场景（战斗 RNG，多人端可同步）
        // 每个场景各自烘焙底部锚点，底部对齐
        if (base.Rng.NextBool())
        {
            return NodeFactory<NCreatureVisuals>.CreateFromScene(AlternateMushroomScenePath);
        }

        return base.CreateCustomVisuals();
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
