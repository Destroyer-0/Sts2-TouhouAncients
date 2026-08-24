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
/// 紫苑的厄运字符爆发特效（厄 / 貧 / 損 / 負）。
/// 一次性粒子爆发，每颗粒子通过图集动画随机显示一个字符，播完自动删除。
/// 挂载在 res://images/sprite/shion/shion_negative_burst.tscn 根节点。
/// </summary>
public partial class NShionNegativeBurstVfx : Node2D
{
    /// <summary>
    /// 场景内部路径（相对 res:// 的完整路径，不走 SceneHelper 的 scenes/ 前缀）。
    /// </summary>
    public const string ScenePath = "res://images/sprite/shion/shion_negative_burst.tscn";

    private CancellationTokenSource? _cts;

    private GpuParticles2D[] _emitters = [];

    private int _count;

    private float _scatterRadius;

    /// <summary>
    /// 创建特效节点。配置数量与散射半径，加入场景树后由 _Ready 自动播放，播完自动删除。
    /// </summary>
    public static NShionNegativeBurstVfx? Create(Vector2 origin, int count, float scatterRadius)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        NShionNegativeBurstVfx vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NShionNegativeBurstVfx>(PackedScene.GenEditState.Disabled);
        vfx.GlobalPosition = origin;
        vfx._count = count;
        vfx._scatterRadius = scatterRadius;
        return vfx;
    }

    public override void _Ready()
    {
        _emitters = GetChildren().OfType<GpuParticles2D>().ToArray();
        Configure(_count, _scatterRadius);
        TaskHelper.RunSafely(PlaySequence());
    }

    public override void _ExitTree()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// 设置粒子总量与散射初速度。
    /// </summary>
    private void Configure(int count, float scatterRadius)
    {
        foreach (GpuParticles2D particles in _emitters)
        {
            particles.Amount = Mathf.Max(1, count);
            if (particles.ProcessMaterial is ParticleProcessMaterial material)
            {
                material.InitialVelocityMin = scatterRadius * 0.55f;
                material.InitialVelocityMax = scatterRadius;
            }
        }
    }

    private async Task PlaySequence()
    {
        _cts = new CancellationTokenSource();
        float lifetime = 0f;
        foreach (GpuParticles2D particles in _emitters)
        {
            particles.Restart();
            particles.Emitting = true;
            lifetime = Mathf.Max(lifetime, (float)particles.Lifetime);
        }

        await Cmd.Wait(lifetime + 1f, _cts.Token);
        this.QueueFreeSafely();
    }
}
