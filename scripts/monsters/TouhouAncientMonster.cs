using BaseLib.Abstracts;
using Godot;

namespace TouhouAncients.Scripts.monsters;

public abstract class TouhouAncientMonster : CustomMonsterModel
{
    /// <summary>
    /// 受击时播放一轮 hurt 动画后回到 idle。
    /// </summary>
    // public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
    //     DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    // {
    //     if (target == base.Creature && props.IsCardOrMonsterMove())
    //     {
    //         PlayAnim("hurt");
    //         await Cmd.Wait(1f);
    //         PlayAnim("idle_loop");
    //     }
    // }

    // --- 技能方法 ---
    private AnimatedSprite2D? _animatedSprite2D;

    public AnimatedSprite2D MyAnimatedSprite2D
    {
        get
        {
            if (_animatedSprite2D == null)
            {
                var body = base.Creature.GetCreatureNode()?.Visuals.GetCurrentBody();
                if (body is AnimatedSprite2D sprite)
                {
                    _animatedSprite2D = sprite;
                }
            }

            return _animatedSprite2D;
        }
    }
    
    protected void PlayAnimation(string animationName)
    {
        MyAnimatedSprite2D.Animation = animationName;
        MyAnimatedSprite2D.Play();
    }
}