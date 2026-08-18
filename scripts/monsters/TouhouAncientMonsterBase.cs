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

    /// <summary>
    /// 是否拥有帧动画（AnimatedSprite2D）。
    /// 纯静态贴图怪物（如奇幻蘑菇）应重写为 false，跳过所有 AnimatedSprite2D 相关处理。
    /// </summary>
    protected virtual bool HasAnimation => true;

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

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public override NCreatureVisuals? CreateCustomVisuals()
    {
        string scenePath = $"res://scenes/creature_visuals/{GetType().Name}.tscn";
        var visuals = NodeFactory<NCreatureVisuals>.CreateFromScene(scenePath);

        if (HasAnimation && visuals.GetNodeOrNull<AnimatedSprite2D>("%Visuals") is AnimatedSprite2D sprite)
        {
            _animatedSprite2D = sprite;
            sprite.AnimationFinished += OnAnimationFinished;
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
    /// 当前状态下使用的常态循环动画。
    /// </summary>
    protected virtual string CurrentLoopAnimation => "idle";

    /// <summary>
    /// 是否允许游戏的 Hit 触发器播放 hurt 动画。
    /// 特殊状态下可重写此属性以保持当前动画。
    /// </summary>
    public virtual bool ShouldPlayHurtAnimation => true;

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
            PlayAnimation("die");
    }

    /// <summary>
    /// 创建绑定到显示节点的位移 Tween。死后会立刻杀掉并锁回原位，避免冲锋半路把尸体带走。
    /// </summary>
    protected Tween CreateBodyMoveTween(Node2D body)
    {
        _bodyMoveTween?.Kill();
        if (IsCreatureDead)
        {
            body.Position = Vector2.Zero;
            Tween killed = body.CreateTween();
            killed.Kill();
            _bodyMoveTween = null;
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

    /// <summary>
    /// 返回指定非循环动画播放结束后应衔接的动画。
    /// 返回 null 表示不自动切换。
    /// </summary>
    protected virtual string? GetNextAnimation(string finishedAnimation)
    {
        if (IsDeathAnimationLocked)
            return null;
        return finishedAnimation == "hurt" ? CurrentLoopAnimation : null;
    }

    protected virtual void PlayAnimation(string animationName)
    {
        if (!HasAnimation) return;
        if (IsDeathAnimationLocked && animationName != "die")
            return;
        MyAnimatedSprite2D.Animation = animationName;
        MyAnimatedSprite2D.Play();
    }

    protected void PlayCurrentLoopAnimation()
    {
        PlayAnimation(CurrentLoopAnimation);
    }

    internal void HandleHitAnimationTrigger()
    {
        if (!HasAnimation || !ShouldPlayHurtAnimation || IsDeathAnimationLocked)
            return;

        if (MyAnimatedSprite2D.SpriteFrames.HasAnimation("hurt"))
            PlayAnimation("hurt");
    }

    private void OnAnimationFinished()
    {
        string? nextAnimation = GetNextAnimation(MyAnimatedSprite2D.Animation);
        if (nextAnimation != null)
            PlayAnimation(nextAnimation);
    }
}
