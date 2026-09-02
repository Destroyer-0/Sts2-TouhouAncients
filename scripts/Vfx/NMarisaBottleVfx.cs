using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.TestSupport;

namespace TouhouAncients.Scripts.Vfx;

/// <summary>
/// 魔理沙的黑洞边缘瓶子抛射特效（shot_fb.png，抛物线投掷 + 持续旋转）。
/// 调用方给出起点与落点，特效沿两点间抛物线飞行并持续旋转（瓶口朝飞行方向自然翻滚），
/// 到达落点后通过 <see cref="Arrived"/> 通知调用方（由调用方播放命中演出并销毁本节点）。
/// 挂载在 res://images/sprite/marisa/marisa_bottle.tscn 根节点。
/// </summary>
public partial class NMarisaBottleVfx : Node2D
{
    /// <summary>
    /// 场景内部路径（相对 res:// 的完整路径，不走 SceneHelper 的 scenes/ 前缀）。
    /// </summary>
    public const string ScenePath = "res://images/sprite/marisa/marisa_bottle.tscn";

    /// <summary>到达落点所需飞行耗时（1 秒，与黑洞边缘攻击的 WithWaitBeforeHit(1f,1f) 对齐，保证瓶子命中与伤害结算同步）。</summary>
    public const float FlightTime = 0.9f;

    /// <summary>抛物线峰值高度（像素，相对连线中点）。</summary>
    private const float ArcHeight = -480f;

    /// <summary>飞行总旋转角度（约 1.5 圈），与 FlightTime 一起决定角速度。</summary>
    private const float TotalSpinDegrees = 1080f;

    private Sprite2D? _bottleSprite;

    private GpuParticles2D? _trail;

    private Vector2 _source;

    private Vector2 _target;

    private CancellationTokenSource? _cts;

    /// <summary>
    /// 创建瓶子抛射特效节点。加入场景树后自动开始飞行，到达落点触发 <see cref="Arrived"/>。
    /// </summary>
    public static NMarisaBottleVfx? Create(Vector2 source, Vector2 target)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        NMarisaBottleVfx vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NMarisaBottleVfx>(PackedScene.GenEditState.Disabled);
        vfx._source = source;
        vfx._target = target;
        return vfx;
    }

    public override void _Ready()
    {
        _bottleSprite = GetNodeOrNull<Sprite2D>("Bottle");
        _trail = GetChildren().OfType<GpuParticles2D>().FirstOrDefault();
        GlobalPosition = _source;

        TaskHelper.RunSafely(Fly());
    }

    public override void _ExitTree()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private async Task Fly()
    {
        _cts = new CancellationTokenSource();
        _trail?.Restart();
        _trail?.SetEmitting(true);

        double elapsed = 0.0;
        while (elapsed < FlightTime && !_cts.IsCancellationRequested)
        {
            float t = (float)(elapsed / FlightTime);
            // 水平匀速 + 垂直正弦弧线：起点/终点高度相同，中间达到峰值 ArcHeight
            // 用 GlobalPosition 赋值，容器（CombatVfxContainer）是否原点对齐不影响轨迹
            float arc = Mathf.Sin(t * Mathf.Pi);
            GlobalPosition = _source.Lerp(_target, t) - Vector2.Up * (arc * ArcHeight);
            // 瓶子自身持续翻滚：全程转过 TotalSpinDegrees（瓶身绕自身中心旋转，拖尾不受影响）
            if (_bottleSprite != null)
            {
                _bottleSprite.RotationDegrees = t * TotalSpinDegrees;
            }
            float delta = await this.AwaitProcessFrame();
            elapsed += delta;
        }

        _trail?.SetEmitting(false);
        this.QueueFreeSafely();
    }
}
