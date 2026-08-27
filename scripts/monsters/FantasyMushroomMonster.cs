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

    /// <summary>奇幻蘑菇是魔理沙召唤的随从，不是挑战本体。</summary>
    public override bool IsPrimaryMonster => false;

    /// <summary>
    /// 备用小蘑菇场景路径（每个场景各自烘焙底部锚点）。
    /// </summary>
    private const string AlternateMushroomScenePath = "res://scenes/creature_visuals/FantasyMushroomAlt.tscn";

    // --- HP ---
    /// <summary>
    /// 初始生命：二层数值（随魔理沙最早出现的幕，也作为图鉴预览等环境的回退值）。
    /// 三层时在 <see cref="AfterAddedToRoom"/> 中提升，因为 Creature 构造函数读取
    /// MinInitialHp/MaxInitialHp 时 Creature 尚未绑定，无法获取幕号。
    /// </summary>
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 31, 28);

    /// <summary>三层初始生命（当前数值）。</summary>
    private int InitialHpAct3 => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 53, 48);

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Plant;
    
    // --- 数值 ---
    private int HealAmount => GetActValue(3, (3, 6));

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // 三层提升初始生命到三层数值（Creature 构造阶段无法获取幕号，故在此调整）。
        // 使用内部 API 直接调整，避免 GainMaxHp 触发 AfterGainMaxHp 等 Hook
        if (CurrentActNumber == 3 && InitialHpAct3 > InitialHp)
        {
            base.Creature.SetMaxHpInternal(InitialHpAct3);
            base.Creature.SetCurrentHpInternal(InitialHpAct3);
        }
        await PowerCmd.Apply<MinionPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
        await PowerCmd.Apply<FungalPower>(new ThrowingPlayerChoiceContext(), base.Creature, (decimal)GetActValue(2, (3, 3)), base.Creature, null);
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
