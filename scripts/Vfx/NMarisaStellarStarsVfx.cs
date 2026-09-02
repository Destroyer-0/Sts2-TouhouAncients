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
/// 魔理沙的星辰幻想星星弹幕特效（star_blue.png 粒子，一次性齐射）。
/// 发射 4~8 颗蓝色星星向左超高速直线飞行，每颗粒子自身以 ±1440°/s 不断旋转，
/// 飞行中缓慢缩小并在寿命末段淡出。播完自动删除。
/// 挂载在 res://images/sprite/marisa/marisa_stellar_stars.tscn 根节点。
/// </summary>
public partial class NMarisaStellarStarsVfx : Node2D
{
    /// <summary>
    /// 场景内部路径（相对 res:// 的完整路径，不走 SceneHelper 的 scenes/ 前缀）。
    /// </summary>
    public const string ScenePath = "res://images/sprite/marisa/marisa_stellar_stars.tscn";

    /// <summary>发射的星星数量（调用方预生成 4~8 随机）。</summary>
    private int _starCount = 8;

    private CancellationTokenSource? _cts;

    private GpuParticles2D? _emitter;

    /// <summary>
    /// 创建星星弹幕特效节点。加入场景树后由 _Ready 自动播放，播完自动删除。
    /// </summary>
    /// <param name="origin">发射原点（全局坐标，通常为魔理沙的 VfxSpawnPosition）。</param>
    /// <param name="starCount">星星数量（4~8）。</param>
    public static NMarisaStellarStarsVfx? Create(Vector2 origin, int starCount)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        NMarisaStellarStarsVfx vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NMarisaStellarStarsVfx>(PackedScene.GenEditState.Disabled);
        vfx.GlobalPosition = origin;
        vfx._starCount = Mathf.Clamp(starCount, 1, 8);
        return vfx;
    }

    public override void _Ready()
    {
        _emitter = GetChildren().OfType<GpuParticles2D>().FirstOrDefault();
        if (_emitter != null)
        {
            // 总粒子数按 4~8 随机（与动画帧数无关，这里不是图集动画）
            _emitter.Amount = Mathf.Max(1, _starCount);
            _emitter.Restart();
            _emitter.Emitting = true;
        }

        TaskHelper.RunSafely(PlaySequence());
    }

    public override void _ExitTree()
    {
        _cts?.Cancel();
    }

    private async Task PlaySequence()
    {
        _cts = new CancellationTokenSource();
        float lifetime = _emitter != null ? (float)_emitter.Lifetime : 0f;

        // 寿命 + 余量后自删（粒子飞行中自然消亡，脚本仅负责收尾）
        await Cmd.Wait(lifetime + 1f, _cts.Token);
        this.QueueFreeSafely();
    }
}
