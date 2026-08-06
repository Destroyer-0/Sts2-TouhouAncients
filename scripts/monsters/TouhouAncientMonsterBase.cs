using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace TouhouAncients.Scripts.monsters;

public abstract class TouhouAncientMonsterBase : CustomMonsterModel
{
    private AnimatedSprite2D? _animatedSprite2D;

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
        return finishedAnimation == "hurt" ? CurrentLoopAnimation : null;
    }

    protected virtual void PlayAnimation(string animationName)
    {
        if (!HasAnimation) return;
        MyAnimatedSprite2D.Animation = animationName;
        MyAnimatedSprite2D.Play();
    }

    protected void PlayCurrentLoopAnimation()
    {
        PlayAnimation(CurrentLoopAnimation);
    }

    internal void HandleHitAnimationTrigger()
    {
        if (!HasAnimation || !ShouldPlayHurtAnimation)
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
