using System.Threading;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace TouhouAncients.Scripts.monsters;

public abstract class TouhouAncientMonsterBase : CustomMonsterModel
{
    private AnimatedSprite2D? _animatedSprite2D;
    private Tween? _bodyMoveTween;
    private CancellationTokenSource? _moveCts;
    private MonsterAnimationStateMachine? _animationMachine;

    /// <summary>
    /// 是否拥有帧动画（AnimatedSprite2D）。
    /// 纯静态贴图怪物（如奇幻蘑菇）应重写为 false，跳过所有 AnimatedSprite2D 相关处理。
    /// </summary>
    protected virtual bool HasAnimation => true;

    /// <summary>
    /// 是否为挑战本体（主怪）。
    /// 召唤的随从（如奇幻蘑菇）应重写为 false，用于图鉴金色标题等展示逻辑。
    /// </summary>
    public virtual bool IsPrimaryMonster => true;

    /// <summary>
    /// Boss 的固定初始生命值。最小生命与最大生命统一使用此值。
    /// </summary>
    protected abstract int InitialHp { get; }

    public override int MinInitialHp => InitialHp;

    public override int MaxInitialHp => InitialHp;

    /// <summary>
    /// 当前幕号（从 1 开始，第二幕为 2，第三幕为 3）。
    /// 通过怪物自身的 Creature 获取战斗状态中的幕号；
    /// canonical 实例（资源加载 GetIntents、图鉴预览等）无法获取时返回 -1，此时按幕取值回退到默认值。
    /// 注意：本属性不能用于 <see cref="InitialHp"/>——Creature 构造函数读取 MinInitialHp/MaxInitialHp
    /// 时 Monster.Creature 尚未绑定（构造函数在读取 HP 之后才设置），且 CombatManager 的 _state
    /// 也在 creatures 创建之后（SetUpCombat）才设置，因此 HP 只能在 AfterAddedToRoom 中按幕调整。
    /// </summary>
    protected int CurrentActNumber
    {
        get
        {
            if (!base.IsMutable) return -1;
            try
            {
                return base.Creature.CombatState?.RunState.CurrentActIndex + 1 ?? -1;
            }
            catch (InvalidOperationException)
            {
                // canonical 实例 / 图鉴 SetUpForCombat 阶段：Creature 尚未绑定，回退到默认值
                return -1;
            }
        }
    }

    /// <summary>
    /// 按当前幕号返回对应数值：actValues 中与当前幕匹配的条目生效，
    /// 未配置的幕（或无法获取幕号的环境）回退到 fallback。
    /// 约定：fallback 使用角色最早出现的幕的数值（如魔理沙最早在第二幕出现，
    /// 默认即为第二幕数值），后续幕（如第三幕）再额外配置更高数值。
    /// 注意：使用按幕数值的 Intent 请改用延迟求值构造（如 new SingleAttackIntent(() => Damage)），
    /// 避免在 canonical 实例的 GenerateMoveStateMachine 中提前求值导致无法区分幕。
    /// </summary>
    protected int GetActValue(int fallback, params (int actNumber, int value)[] actValues)
    {
        int actNumber = CurrentActNumber;
        foreach ((int act, int value) in actValues)
        {
            if (act == actNumber) return value;
        }
        return fallback;
    }

    /// <summary>
    /// 按幕数覆写初始生命：将怪物的最大生命与当前生命设为指定幕的数值，
    /// 并重新应用多人模式血量缩放（生命 × 玩家数 × 幕缩放系数）。
    /// 必须在 <see cref="AfterAddedToRoom"/> 中调用——Creature 构造函数读取
    /// MinInitialHp/MaxInitialHp 时 CombatState 尚未绑定、无法获取幕号，
    /// 而多人缩放已在 CombatState.CreateCreature 时基于默认生命执行过，
    /// 此处覆写后若不重新缩放，多人翻倍的生命会被重置为固定值。
    /// 内部通过原版 ScaleMonsterHpForMultiplayer 缩放，单机（playerCount == 1）自动跳过。
    /// </summary>
    /// <param name="actHp">当前幕的目标生命值。</param>
    protected void SetActInitialHp(int actHp)
    {
        base.Creature.SetMaxHpInternal(actHp);
        base.Creature.SetCurrentHpInternal(actHp);

        if (base.Creature.CombatState is { } combatState)
        {
            base.Creature.ScaleMonsterHpForMultiplayer(
                combatState.Encounter,
                combatState.Players.Count,
                combatState.RunState.CurrentActIndex);
        }
    }

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public override NCreatureVisuals? CreateCustomVisuals()
    {
        string scenePath = $"res://scenes/creature_visuals/{GetType().Name}.tscn";
        var visuals = NodeFactory<NCreatureVisuals>.CreateFromScene(scenePath);

        if (HasAnimation && visuals.GetNodeOrNull<AnimatedSprite2D>("%Visuals") is AnimatedSprite2D sprite)
        {
            _animatedSprite2D = sprite;
        }

        return visuals;
    }

    public AnimatedSprite2D MyAnimatedSprite2D
    {
        get
        {
            // 无帧动画的怪物不尝试获取 AnimatedSprite2D
            if (!HasAnimation) return null!;

            if (_animatedSprite2D == null)
            {
                var body = base.Creature.GetCreatureNode()?.Visuals.GetCurrentBody();
                if (body is AnimatedSprite2D sprite)
                {
                    _animatedSprite2D = sprite;
                }
            }

            return _animatedSprite2D!;
        }
    }

    /// <summary>
    /// 动画状态机（懒构建）。子类通过 <see cref="ConfigureAnimationStateMachine"/> 注册自定义动画状态；
    /// 所有动画控制统一走 Anim.Trigger / Anim.TriggerLoop。
    /// </summary>
    protected MonsterAnimationStateMachine Anim
    {
        get
        {
            if (_animationMachine == null)
            {
                _animationMachine = new MonsterAnimationStateMachine(MyAnimatedSprite2D)
                {
                    IsDeathLocked = () => IsDeathAnimationLocked,
                };
                // 默认状态表：idle 循环；hurt 一次性（播完由状态机内部回到打断前状态）；die 一次性
                _animationMachine.RegisterLoop("idle");
                _animationMachine.RegisterOneShot("hurt");
                _animationMachine.RegisterOneShot("die");
                ConfigureAnimationStateMachine(_animationMachine);
            }
            return _animationMachine;
        }
    }

    /// <summary>
    /// 子类在此注册自定义动画状态（循环 / 一次性 / 完成转移 / 进入退出钩子）。
    /// 例如：animationMachine.RegisterLoop("dash"); animationMachine.RegisterOneShot("throw");
    /// </summary>
    protected virtual void ConfigureAnimationStateMachine(MonsterAnimationStateMachine animationMachine) { }

    /// <summary>
    /// 死后仍要播非 die 动画时返回 true（例如紫苑双生倒地等待复活）。
    /// </summary>
    protected virtual bool ShouldKeepAnimatingWhileDead => false;

    private bool IsCreatureDead
    {
        get
        {
            if (!IsMutable)
                return false;
            try
            {
                return Creature.IsDead;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 死后锁定为 die，忽略 hurt / idle / 技能动画。
    /// </summary>
    protected bool IsDeathAnimationLocked => HasAnimation && IsCreatureDead && !ShouldKeepAnimatingWhileDead;

    public override async Task BeforeDeath(Creature creature)
    {
        await base.BeforeDeath(creature);
        if (creature != Creature || !HasAnimation)
            return;

        StopBodyMoveAndResetPosition();
        if (!ShouldKeepAnimatingWhileDead)
            Anim.Trigger("die");
    }

    /// <summary>
    /// 供子类位移 Tween 等待用（await tween.AwaitFinished(MoveCancellationToken)）。
    /// 死亡 / 创建新位移 Tween 时取消，防止协程卡死：
    /// Godot 的 Tween.Kill() 不触发 Finished 信号，若 Move 协程挂在 AwaitFinished 上会永远等待；
    /// 通过 CancellationToken 主动取消，等待方立即返回。
    /// </summary>
    protected CancellationToken MoveCancellationToken => _moveCts?.Token ?? CancellationToken.None;

    /// <summary>
    /// 创建绑定到显示节点的位移 Tween。死后会立刻杀掉并锁回原位，避免冲锋半路把尸体带走；
    /// 创建新 Tween 时取消旧的等待（多段位移 Tween 前一个被 Kill 也不会卡死等待方）。
    /// </summary>
    protected Tween CreateBodyMoveTween(Node2D body)
    {
        _moveCts?.Cancel();
        _moveCts?.Dispose();
        _moveCts = new CancellationTokenSource();

        _bodyMoveTween?.Kill();
        if (IsCreatureDead)
        {
            body.Position = Vector2.Zero;
            _bodyMoveTween = null;
            Tween killed = body.CreateTween();
            killed.Kill();
            return killed;
        }

        _bodyMoveTween = body.CreateTween();
        return _bodyMoveTween;
    }

    /// <summary>
    /// 设置显示节点坐标。死后忽略写入，保持站位。
    /// </summary>
    protected void SetBodyPosition(Node2D body, Vector2 position)
    {
        if (IsCreatureDead)
        {
            body.Position = Vector2.Zero;
            return;
        }

        body.Position = position;
    }

    private void StopBodyMoveAndResetPosition()
    {
        // 死亡瞬间：所有等待中的位移 Tween 协程立即返回，防止协程卡死
        _moveCts?.Cancel();
        _moveCts?.Dispose();
        _moveCts = null;

        _bodyMoveTween?.Kill();
        _bodyMoveTween = null;

        Node2D? body = Creature.GetCreatureNode()?.Visuals?.GetCurrentBody();
        if (body != null)
            body.Position = Vector2.Zero;
    }

    // public override async Task AfterAddedToRoom()
    // {
    //     await base.AfterAddedToRoom();
    //     MyAnimatedSprite2D.AnimationFinished += OnAnimationFinished;
    // }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        //MyAnimatedSprite2D.AnimationFinished += OnAnimationFinished;
        return base.GenerateAnimator(controller);
    }

    internal void HandleHitAnimationTrigger()
    {
        if (!HasAnimation) return;
        Anim.HandleHit();
    }
}
