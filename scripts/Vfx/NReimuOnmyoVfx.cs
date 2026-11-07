using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace TouhouAncients.Scripts.Vfx;

/// <summary>
/// 灵梦「阴阳玉」的投掷物：onmyo_big 沿抛物线飞出、在地上反弹一次后砸向目标，
/// 命中瞬间 <see cref="PlayAsync"/> 返回；随后 0.3 秒内放大并渐隐表示消失。
/// 挂载在 res://images/sprite/reimu/reimu_onmyo.tscn 根节点。
/// </summary>
public partial class NReimuOnmyoVfx : Node2D
{
    /// <summary>场景内部路径（相对 res:// 的完整路径，不走 SceneHelper 的 scenes/ 前缀）。</summary>
    public const string ScenePath = "res://images/sprite/reimu/reimu_onmyo.tscn";

    /// <summary>第一段（出手到落地）飞行耗时。</summary>
    private const float FirstHopSeconds = 0.45f;

    /// <summary>第二段（弹起后砸向目标）飞行耗时。</summary>
    private const float SecondHopSeconds = 0.35f;

    /// <summary>两段抛物的弧高。</summary>
    private const float FirstArcHeight = 260f;
    private const float SecondArcHeight = 150f;

    /// <summary>反弹点相对玩家中心再往下一点（y 向下为正），落在脚底附近。</summary>
    private const float GroundOffsetY = 110f;

    private Sprite2D? _orb;

    private GpuParticles2D? _trail;

    /// <summary>累计旋转角度（度）：两段抛物各自用 t 会每段重置，必须跨段累加。</summary>
    private float _spinDegrees;

    /// <summary>旋转角速度（度/秒）。</summary>
    private const float SpinDegreesPerSecond = 900f;

    /// <summary>消失特效放大的终值倍率（相对场景里 Orb 的 scale 1.6）。</summary>
    private const float ShrinkGrowTimes = 2.6f;

    private CancellationTokenSource? _cts;

    private Vector2 _source;

    private Vector2 _target;

    /// <summary>创建阴阳玉投掷物。起点为施法者，终点为受击目标。</summary>
    public static NReimuOnmyoVfx? Create(Vector2 source, Vector2 target)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        NReimuOnmyoVfx vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NReimuOnmyoVfx>(PackedScene.GenEditState.Disabled);
        vfx._source = source;
        vfx._target = target;
        return vfx;
    }

    public override void _Ready()
    {
        GlobalPosition = _source;
        _orb = GetNodeOrNull<Sprite2D>("Orb");
        _trail = GetNodeOrNull<GpuParticles2D>("Trail");
    }

    public override void _ExitTree()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <summary>抛物线 → 反弹 → 砸向目标。命中瞬间返回，之后自行放大渐隐销毁。</summary>
    public async Task PlayAsync()
    {
        var cts = new CancellationTokenSource();
        _cts = cts;

        // 反弹点贴在玩家脚下：Godot 里 y 向下为正，+GroundOffsetY 就是落到脚底
        float groundY = _target.Y + GroundOffsetY;
        Vector2 bounce = new(Mathf.Lerp(_source.X, _target.X, 0.45f), groundY);

        await FlySegmentAsync(_source, bounce, FirstArcHeight, FirstHopSeconds, cts.Token);
        if (cts.IsCancellationRequested)
            return;

        // 反弹后高高跳起再砸下来
        await FlySegmentAsync(bounce, _target, SecondArcHeight, SecondHopSeconds, cts.Token);
        if (cts.IsCancellationRequested)
            return;

        if (_trail != null)
        {
            _trail.Emitting = false;
        }

        TaskHelper.RunSafely(ShrinkAndFreeAsync(cts.Token));
    }

    /// <summary>一段抛物线：水平匀速 + 向上拱起的正弦弧（Godot 的 y 向下为正，所以用 +Vector2.Up）。</summary>
    private async Task FlySegmentAsync(Vector2 from, Vector2 to, float arcHeight, float seconds,
        CancellationToken token)
    {
        double elapsed = 0.0;
        while (elapsed < seconds && !token.IsCancellationRequested)
        {
            float delta = await this.AwaitProcessFrame();
            elapsed += delta;
            float t = Mathf.Clamp((float)(elapsed / seconds), 0f, 1f);

            GlobalPosition = from.Lerp(to, t) + Vector2.Up * (Mathf.Sin(t * Mathf.Pi) * arcHeight);
            if (_orb != null)
            {
                _spinDegrees += SpinDegreesPerSecond * delta;
                _orb.RotationDegrees = _spinDegrees;
            }
        }
    }

    /// <summary>命中后的消失特效：0.3 秒内快速扩大并渐隐。</summary>
    private async Task ShrinkAndFreeAsync(CancellationToken token)
    {
        if (_orb != null && GodotObject.IsInstanceValid(_orb))
        {
            Tween tween = _orb.CreateTween().SetParallel();
            tween.TweenProperty(_orb, "scale", _orb.Scale * ShrinkGrowTimes, 0.3f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            tween.TweenProperty(_orb, "modulate:a", 0f, 0.3f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        }

        await Cmd.Wait(0.32f, token);
        this.QueueFreeSafely();
    }
}
