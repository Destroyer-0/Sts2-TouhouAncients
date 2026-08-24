using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

namespace TouhouAncients.Scripts.Vfx;

/// <summary>
/// 女苑的财富字符特效（金 / 富 / 豊 / 宝）。
/// 持续发射，每颗粒子通过图集动画随机显示一个字符（混合出现），由调用方通过 Start / Stop 控制，停止后自动清理。
/// 挂载在 res://images/sprite/joon/joon_wealth_burst.tscn 根节点。
/// </summary>
public partial class NJoonWealthBurstVfx : Node2D
{
    /// <summary>
    /// 场景内部路径（相对 res:// 的完整路径，不走 SceneHelper 的 scenes/ 前缀）。
    /// </summary>
    public const string ScenePath = "res://images/sprite/joon/joon_wealth_burst.tscn";

    private CancellationTokenSource? _cts;

    private GpuParticles2D[] _emitters = [];

    private bool _playing;

    private bool _stopped;

    /// <summary>
    /// 创建特效节点。加入场景树后调用 Start() 开始持续发射。
    /// </summary>
    public static NJoonWealthBurstVfx? Create(Vector2 position)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        NJoonWealthBurstVfx vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NJoonWealthBurstVfx>(PackedScene.GenEditState.Disabled);
        vfx.GlobalPosition = position;
        return vfx;
    }

    public override void _Ready()
    {
        _emitters = GetChildren().OfType<GpuParticles2D>().ToArray();
    }

    public override void _ExitTree()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// 开始持续发射。每个粒子通过图集动画随机显示一个字符（由 tscn 配置控制）。
    /// </summary>
    public void Start()
    {
        if (_playing || _stopped)
            return;

        _playing = true;
        foreach (GpuParticles2D particles in _emitters)
            particles.Emitting = true;
    }

    /// <summary>
    /// 停止发射，等待粒子播完后自动删除节点。
    /// </summary>
    public void Stop()
    {
        if (_stopped)
            return;

        _stopped = true;
        _playing = false;
        foreach (GpuParticles2D particles in _emitters)
            particles.Emitting = false;

        TaskHelper.RunSafely(WaitForFinishAndFree());
    }

    /// <summary>
    /// 播放指定秒数后自动停止并清理。需在加入场景树后调用。
    /// </summary>
    public void PlayForSeconds(float seconds)
    {
        Start();
        TaskHelper.RunSafely(StopAfter(seconds));
    }

    private async Task StopAfter(float seconds)
    {
        await Cmd.Wait(seconds);
        Stop();
    }

    private async Task WaitForFinishAndFree()
    {
        _cts = new CancellationTokenSource();
        float lifetime = 0f;
        foreach (GpuParticles2D particles in _emitters)
            lifetime = Mathf.Max(lifetime, (float)particles.Lifetime);

        await Cmd.Wait(lifetime + 0.2f, _cts.Token);
        this.QueueFreeSafely();
    }
}
