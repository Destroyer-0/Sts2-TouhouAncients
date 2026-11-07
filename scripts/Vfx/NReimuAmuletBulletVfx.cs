using System.Collections.Generic;
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
/// 灵梦「梦想封印」的御札弹幕：7 张 amulet_purple 斜排成斜线高速飞向目标，
/// 命中时切换为 amulet_purple2 六帧特效后消失。<see cref="PlayAsync"/> 在全部命中瞬间返回。
/// 挂载在 res://images/sprite/reimu/reimu_amulet_bullets.tscn 根节点。
/// </summary>
public partial class NReimuAmuletBulletVfx : Node2D
{
    /// <summary>场景内部路径（相对 res:// 的完整路径，不走 SceneHelper 的 scenes/ 前缀）。</summary>
    public const string ScenePath = "res://images/sprite/reimu/reimu_amulet_bullets.tscn";

    private const int BulletCount = 7;

    /// <summary>弹幕整体飞行耗时，与高速弹幕的手感一致。</summary>
    private const float FlySeconds = 0.22f;

    /// <summary>斜排间距：垂直于飞行方向的错开距离。</summary>
    private const float SpreadStep = 40f;

    /// <summary>沿飞行方向的错开距离，形成阶梯状斜排。</summary>
    private const float StaggerStep = 26f;

    private readonly List<AnimatedSprite2D> _bullets = new();

    private AnimatedSprite2D? _hitFx;

    private CancellationTokenSource? _cts;

    private Vector2 _source;

    private Vector2 _target;

    /// <summary>创建御札弹幕。起点为施法者，终点为受击目标。</summary>
    public static NReimuAmuletBulletVfx? Create(Vector2 source, Vector2 target)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        NReimuAmuletBulletVfx vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NReimuAmuletBulletVfx>(PackedScene.GenEditState.Disabled);
        vfx._source = source;
        vfx._target = target;
        return vfx;
    }

    public override void _Ready()
    {
        GlobalPosition = _source;
        CollectBullets();
    }

    public override void _ExitTree()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <summary>飞行并在命中时播放命中特效，全部命中后返回。</summary>
    public async Task PlayAsync()
    {
        if (_bullets.Count == 0)
        {
            await Cmd.Wait(FlySeconds);
            PlayHitFx();
            return;
        }

        var cts = new CancellationTokenSource();
        _cts = cts;

        Vector2 direction = (_target - _source).Normalized();
        Vector2 normal = new(-direction.Y, direction.X);

        // 目标点沿飞行方向前移造成的阶梯错位，起手时反向排开
        Vector2[] starts = new Vector2[_bullets.Count];
        Vector2[] ends = new Vector2[_bullets.Count];
        for (int i = 0; i < _bullets.Count; i++)
        {
            float lateral = (i - (BulletCount - 1) * 0.5f) * SpreadStep;
            float along = -i * StaggerStep;
            Vector2 offset = normal * lateral + direction * along;
            starts[i] = _source + offset;
            ends[i] = _target + offset * 0.25f;
            _bullets[i].GlobalPosition = starts[i];
        }

        double elapsed = 0.0;
        while (elapsed < FlySeconds && !cts.IsCancellationRequested)
        {
            float delta = await this.AwaitProcessFrame();
            elapsed += delta;
            float t = Mathf.Clamp((float)(elapsed / FlySeconds), 0f, 1f);
            for (int i = 0; i < _bullets.Count; i++)
            {
                _bullets[i].GlobalPosition = starts[i].Lerp(ends[i], t);
            }
        }

        if (cts.IsCancellationRequested)
            return;

        PlayHitFx();
        foreach (AnimatedSprite2D bullet in _bullets)
        {
            bullet.Visible = false;
        }
        _bullets.Clear();
    }

    /// <summary>取 Bullet0~6 与 HitFx 节点，弹体旋转到飞行方向。</summary>
    private void CollectBullets()
    {
        Vector2 direction = (_target - _source).Normalized();
        float rotation = Mathf.Atan2(direction.Y, direction.X);

        for (int i = 0; i < BulletCount; i++)
        {
            if (GetNodeOrNull<AnimatedSprite2D>($"Bullet{i}") is not { } bullet)
            {
                continue;
            }

            bullet.Rotation = rotation;
            _bullets.Add(bullet);
        }

        _hitFx = GetNodeOrNull<AnimatedSprite2D>("HitFx");
        // 命中特效应与弹体同角度，否则默认朝右会与来弹方向对不上
        if (_hitFx != null)
        {
            _hitFx.Rotation = rotation;
        }
    }

    /// <summary>命中特效：场景内置的 amulet_purple2 六帧，播完连同本节点回收。</summary>
    private void PlayHitFx()
    {
        if (_hitFx != null)
        {
            _hitFx.GlobalPosition = _target;
            _hitFx.Visible = true;
            _hitFx.Play("default");
        }

        // 六帧 @20fps ≈ 0.3 秒，播完连同本节点一起回收
        TaskHelper.RunSafely(FreeAfterAsync(0.35f));
    }

    private async Task FreeAfterAsync(float seconds)
    {
        await Cmd.Wait(seconds);
        this.QueueFreeSafely();
    }
}
