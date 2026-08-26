using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TouhouAncients.Scripts.Vfx;

/// <summary>
/// 灵符燃烧触发特效（amulet_orange2.png 六帧序列帧粒子，一次性爆发）。
/// 挂载在 res://images/sprite/reimu/amuletTrigger.tscn 根节点。
/// 播放完毕后通过 <see cref="PlaybackFinished"/> 回调通知调用方回收（不自行销毁），
/// 以便由 HakureiReimuVisuals 的对象池复用，并随其一起清理。
/// </summary>
public partial class NAmuletTriggerVfx : Node2D
{
    /// <summary>
    /// 场景内部路径（相对 res:// 的完整路径，不走 SceneHelper 的 scenes/ 前缀）。
    /// </summary>
    public const string ScenePath = "res://images/sprite/reimu/amuletTrigger.tscn";

    private CancellationTokenSource? _cts;

    private GpuParticles2D? _emitter;

    /// <summary>播放结束回调（参数为自身，供调用方归还对象池）。</summary>
    public event Action<NAmuletTriggerVfx>? PlaybackFinished;

    /// <summary>创建特效节点。加入场景树后调用 <see cref="PlayAt"/> 开始播放。</summary>
    public static NAmuletTriggerVfx Create()
    {
        return PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NAmuletTriggerVfx>(PackedScene.GenEditState.Disabled);
    }

    public override void _Ready()
    {
        _emitter = GetChildren().OfType<GpuParticles2D>().FirstOrDefault();
    }

    public override void _ExitTree()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// 在指定全局位置播放一次序列帧粒子爆发。
    /// 播放完毕触发 <see cref="PlaybackFinished"/>，由调用方决定回收或销毁。
    /// </summary>
    public void PlayAt(Vector2 globalPosition)
    {
        if (NCombatRoom.Instance?.CombatVfxContainer == null)
        {
            return;
        }

        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(this);
        GlobalPosition = globalPosition;

        if (_emitter == null)
        {
            _emitter = GetChildren().OfType<GpuParticles2D>().FirstOrDefault();
        }
        if (_emitter != null)
        {
            _emitter.Restart();
            _emitter.Emitting = true;
        }

        TaskHelper.RunSafely(PlaySequence());
    }

    private async Task PlaySequence()
    {
        _cts = new CancellationTokenSource();
        float lifetime = _emitter != null ? (float)_emitter.Lifetime : 0f;

        await Cmd.Wait(lifetime + 1f, _cts.Token);
        PlaybackFinished?.Invoke(this);
    }
}
