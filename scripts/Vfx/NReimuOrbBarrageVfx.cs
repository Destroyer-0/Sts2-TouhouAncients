using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace TouhouAncients.Scripts.Vfx;

/// <summary>
/// 灵梦「梦想天生」的六色光球轰炸：从施法者身上召出红橙黄绿蓝紫六颗光球，
/// 以正六边形环绕旋转 1.5 秒后齐射目标并爆炸（震屏），随后自行渐隐销毁。
/// <see cref="PlayAsync"/> 在爆炸瞬间返回，方便调用方在同一帧结算伤害。
/// 挂载在 res://images/sprite/reimu/reimu_orb_barrage.tscn 根节点。
/// </summary>
public partial class NReimuOrbBarrageVfx : Node2D
{
    /// <summary>场景内部路径（相对 res:// 的完整路径，不走 SceneHelper 的 scenes/ 前缀）。</summary>
    public const string ScenePath = "res://images/sprite/reimu/reimu_orb_barrage.tscn";

    /// <summary>环绕半径（像素）。</summary>
    private const float OrbitRadius = 270f;

    /// <summary>纵向压扁系数，让六边形看起来是斜躺的环。</summary>
    private const float OrbitSquash = 1f;

    private const float GrowSeconds = 0.4f;
    private const float SpinSeconds = 1.5f;
    private const float FlySeconds = 0.25f;
    private const float SpinSpeedDegrees = 220f;

    /// <summary>光球终态缩放（生长阶段从 0 缓入到这个值）。</summary>
    private const float OrbScale = 2.2f;

    private static readonly Color[] OrbColors =
    {
        new(1f, 0.15f, 0.15f), // 红
        new(1f, 0.55f, 0.1f),  // 橙
        new(1f, 0.95f, 0.2f),  // 黄
        new(0.2f, 0.95f, 0.35f), // 绿
        new(0.25f, 0.6f, 1f),  // 蓝
        new(0.75f, 0.35f, 1f), // 紫
    };

    private readonly List<Sprite2D> _orbs = new();

    /// <summary>与 <see cref="_orbs"/> 一一对应的彗尾（Line2D + shaders/trail_main.png 拉伸）。</summary>
    private readonly List<Line2D> _trails = new();

    /// <summary>上一帧位置，用来判断是否真的在动。</summary>
    private readonly List<Vector2> _lastPositions = new();

    /// <summary>彗尾保留的点数：越多越长；按帧推点，长度跟速度走。</summary>
    private const int TrailPointCount = 18;

    /// <summary>速度低于此值（像素/秒）视为静止，不推点（生长阶段六球挤在原点）。</summary>
    private const float TrailMinSpeed = 60f;

    private CancellationTokenSource? _cts;

    private Vector2 _source;

    private Vector2 _target;

    /// <summary>光球开始齐射（发射瞬间）的回调，供施术者切施法姿态。</summary>
    public Action? VolleyStarting { get; set; }

    /// <summary>创建光球轰炸特效。起点为施法者出生点，终点为受击目标位置。</summary>
    public static NReimuOrbBarrageVfx? Create(Vector2 source, Vector2 target)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        NReimuOrbBarrageVfx vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NReimuOrbBarrageVfx>(PackedScene.GenEditState.Disabled);
        vfx._source = source;
        vfx._target = target;
        return vfx;
    }

    public override void _Ready()
    {
        GlobalPosition = _source;
        CollectOrbs();
    }

    public override void _ExitTree()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <summary>依次播放：召出并环绕 → 齐射 → 爆炸。爆炸瞬间返回。</summary>
    public async Task PlayAsync()
    {
        var cts = new CancellationTokenSource();
        _cts = cts;

        await GatherAndSpinAsync(cts.Token);
        if (cts.IsCancellationRequested)
            return;

        VolleyStarting?.Invoke();

        await FlyToTargetAsync(cts.Token);
        if (cts.IsCancellationRequested)
            return;

        Explode();
        // 爆炸后淡出收尾：不阻塞调用方结算伤害
        _ = TaskHelper.RunSafely(FadeAndFreeAsync(cts.Token));
    }

    /// <summary>从场景取 Orb0~5 及同名彗尾并上色（数量固定 6，颜色随槽位）。</summary>
    private void CollectOrbs()
    {
        for (int i = 0; i < OrbColors.Length; i++)
        {
            if (GetNodeOrNull<Sprite2D>($"Orb{i}") is not { } orb
                || GetNodeOrNull<Line2D>($"Trail{i}") is not { } trail)
            {
                continue;
            }

            orb.Modulate = OrbColors[i];
            trail.Modulate = OrbColors[i];
            trail.ClearPoints();
            _orbs.Add(orb);
            _trails.Add(trail);
            _lastPositions.Add(orb.Position);
        }
    }

    /// <summary>
    /// 最新位置追加到末尾、最旧点从 index 0 删。trail_main.png 尖端在 u=0、粗头在 u=1，
    /// 只有新点落在末尾才对得上（反过来会尾巴朝前）。
    /// </summary>
    private void UpdateTrail(int index, Vector2 position, float delta)
    {
        Vector2 previous = _lastPositions[index];
        _lastPositions[index] = position;

        if (delta <= 0f || (position - previous).Length() / delta < TrailMinSpeed)
        {
            return;
        }

        Line2D trail = _trails[index];
        trail.AddPoint(position);
        while (trail.GetPointCount() > TrailPointCount)
        {
            trail.RemovePoint(0);
        }
    }

    /// <summary>环绕阶段：半径与缩放缓入，角度按帧累加。</summary>
    private async Task GatherAndSpinAsync(CancellationToken token)
    {
        double elapsed = 0.0;
        float spin = 0f;
        float total = GrowSeconds + SpinSeconds;

        while (elapsed < total && !token.IsCancellationRequested)
        {
            float delta = await this.AwaitProcessFrame();
            elapsed += delta;
            spin += Mathf.DegToRad(SpinSpeedDegrees) * delta;

            float grow = Mathf.Clamp((float)elapsed / GrowSeconds, 0f, 1f);
            float radius = OrbitRadius * Mathf.Sin(grow * Mathf.Pi * 0.5f);

            for (int i = 0; i < _orbs.Count; i++)
            {
                float angle = spin + i * Mathf.Tau / _orbs.Count;
                Vector2 offset = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * OrbitSquash);
                _orbs[i].Position = offset;
                _orbs[i].Scale = Vector2.One * (OrbScale * grow);
                UpdateTrail(i, offset, delta);
            }
        }
    }

    /// <summary>齐射阶段：从当前环绕位置直线飞向目标。</summary>
    private async Task FlyToTargetAsync(CancellationToken token)
    {
        Vector2 targetLocal = _target - GlobalPosition;
        Vector2[] starts = new Vector2[_orbs.Count];
        for (int i = 0; i < _orbs.Count; i++)
        {
            starts[i] = _orbs[i].Position;
        }

        double elapsed = 0.0;
        while (elapsed < FlySeconds && !token.IsCancellationRequested)
        {
            float delta = await this.AwaitProcessFrame();
            elapsed += delta;
            float t = Mathf.Clamp((float)(elapsed / FlySeconds), 0f, 1f);
            for (int i = 0; i < _orbs.Count; i++)
            {
                Vector2 position = starts[i].Lerp(targetLocal, t);
                _orbs[i].Position = position;
                UpdateTrail(i, position, delta);
            }
        }
    }

    /// <summary>爆炸：命中点火焰爆开 + 震屏。</summary>
    private void Explode()
    {
        NCombatRoom? room = NCombatRoom.Instance;
        if (room?.CombatVfxContainer != null)
        {
            VfxCmd.PlayVfx(_target, "vfx/vfx_fire_burst", room.CombatVfxContainer);
        }

        NGame.Instance?.ScreenShake(ShakeStrength.TooMuch, ShakeDuration.Short);
    }

    private async Task FadeAndFreeAsync(CancellationToken token)
    {
        foreach (Sprite2D orb in _orbs)
        {
            if (GodotObject.IsInstanceValid(orb))
            {
                orb.CreateTween().TweenProperty(orb, "modulate:a", 0f, 0.25f);
            }
        }

        foreach (Line2D trail in _trails)
        {
            if (GodotObject.IsInstanceValid(trail))
            {
                trail.CreateTween().TweenProperty(trail, "modulate:a", 0f, 0.25f);
            }
        }

        await Cmd.Wait(0.3f, token);
        this.QueueFreeSafely();
    }
}
