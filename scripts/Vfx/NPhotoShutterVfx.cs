using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Audio;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.TestSupport;

namespace TouhouAncients.Scripts.Vfx;

/// <summary>
/// 拍照定格演出：准星合焦 → 相框对齐并遮黑 → 白闪 → 判定标签弹出后飞出。
/// 素材是 images/vfx/photo.png、photo2.png 的图集切片，统一放大 4 倍。
/// 演出分两半：<see cref="Closed"/> 在白闪瞬间完成供调用方发牌，<see cref="ShowBadges"/> 才开标签。
/// </summary>
public partial class NPhotoShutterVfx : Control
{
    private const string VfxDirectory = "res://images/vfx/";

    // 素材默认放大倍数
    private const float ArtScale = 4f;

    // 相框角标：photo2 是一个角，四份翻转拼成边框
    private const float CornerScale = ArtScale * 0.5f;

    // 判定标签固定大小
    private const float BadgeScale = 3f;

    private const float FocusSeconds = 1f;

    // 准星淡入用时
    private const float CrosshairFadeSeconds = 0.25f;

    // 准星绕圈：基础角速度保证 1 秒内能绕目标转一圈，再叠随机扰动
    private const float WobbleAngleSpeed = 5.5f;

    private const float WobbleAngleJitter = 2f;

    private const float WobbleRadius = 70f;

    private const float WobbleRadiusJitter = 60f;

    // 准星固定大小
    private const float CrosshairScale = 3f;

    private const float FrameSeconds = 0.25f;

    private const float HoldSeconds = 0.1f;

    private const float FlashSeconds = 0.3f;

    private const float BadgePopSeconds = 0.12f;

    private const float BadgeHoldSeconds = 1f;

    private const float BadgeFlySeconds = 0.35f;

    private const float BadgeFlyDistance = 240f;

    // 相框初现时相对 Bounds 的下移量
    private const float FrameDrop = 25f;

    // 两枚标签纵向叠放的中心距
    private const float BadgeGap = 40f;

    // 标签相对 center 的锚点（左上方向）
    private static readonly Vector2 BadgeAnchor = new(-170f, -180f);

    private static readonly ModSound FocusInSfx = new("res://debug_audio/sfx/se_focusin.wav");

    private static readonly ModSound ShutterSfx = new("res://debug_audio/sfx/se_shutter.wav");

    private const float VanishAt = FocusSeconds + FrameSeconds + HoldSeconds;

    private const float BadgeTotalSeconds = BadgePopSeconds + BadgeHoldSeconds + BadgeFlySeconds;

    private readonly TaskCompletionSource _closed = new();

    private readonly TaskCompletionSource _finished = new();

    private readonly List<(Sprite2D Sprite, Vector2 Origin)> _badges = [];

    private NCreature _target = null!;

    private bool _perfect;

    private bool _isAttack;

    private Sprite2D _crosshair = null!;

    private Sprite2D _circle = null!;

    private Sprite2D[] _corners = [];

    private Vector2 _center;

    private Rect2 _bounds;

    private Rect2 _frame;

    private float _elapsed;

    private float _badgeStart = -1f;

    private readonly RandomNumberGenerator _rng = new();

    private float _wobbleAngle;

    private float _wobbleRadius = WobbleRadius;

    private Vector2 _wobbleOffset;

    private bool _focusStarted;

    private bool _vanished;

    private bool _done;

    /// <summary>遮罩消失、白闪的瞬间。</summary>
    public Task Closed => _closed.Task;

    /// <summary>整段演出结束（标签飞走后）。</summary>
    public Task Finished => _finished.Task;

    public static NPhotoShutterVfx? Create(NCreature target, bool perfect, bool isAttack)
    {
        if (TestMode.IsOn) return null;

        return new NPhotoShutterVfx
        {
            Name = "PhotoShutterVfx",
            _target = target,
            _perfect = perfect,
            _isAttack = isAttack,
            MouseFilter = MouseFilterEnum.Ignore,
            // 不用 SetAnchorsPreset：AddChildSafely 可能延后入树，那时父矩形还是空
            AnchorRight = 1f,
            AnchorBottom = 1f,
        };
    }

    /// <summary>白闪之后才开标签，保证卡牌预览先出发；重复调用无效。</summary>
    public void ShowBadges()
    {
        if (_badgeStart >= 0f) return;
        _badgeStart = _elapsed;

        if (_isAttack)
        {
            AddBadge(_perfect ? "photo6" : "photo5", BadgeGap);
            AddBadge("photo8", 0f);
        }
        else
        {
            AddBadge("photo7", 0f);
        }
    }

    public override void _Ready()
    {
        _crosshair = AddSprite("photo1", CrosshairScale);
        _circle = AddSprite("photo3");
        _corners = [
            AddSprite("photo2", CornerScale),
            AddSprite("photo2", CornerScale),
            AddSprite("photo2", CornerScale),
            AddSprite("photo2", CornerScale),
        ];
        // 0=左上 1=右上 2=左下 3=右下
        _corners[1].FlipH = true;
        _corners[2].FlipV = true;
        _corners[3].FlipH = true;
        _corners[3].FlipV = true;

        SampleTarget();
    }

    public override void _ExitTree()
    {
        // 战斗中断也要放行等待方，否则 OnPlay 会永远卡住
        _closed.TrySetResult();
        _finished.TrySetResult();
    }

    public override void _Process(double delta)
    {
        if (_done) return;

        _elapsed += (float)delta;
        // 先切阶段再刷新，遮罩消失的那帧就不会多显示一次相框
        CheckPhase();
        UpdateVisuals((float)delta);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_done) return;

        Vector2 size = Size;

        if (_elapsed >= FocusSeconds && _elapsed < VanishAt)
        {
            // 相框之外全黑
            DrawRect(new Rect2(0f, 0f, size.X, _frame.Position.Y), Colors.Black);
            DrawRect(new Rect2(0f, _frame.End.Y, size.X, size.Y - _frame.End.Y), Colors.Black);
            DrawRect(new Rect2(0f, _frame.Position.Y, _frame.Position.X, _frame.Size.Y), Colors.Black);
            DrawRect(new Rect2(_frame.End.X, _frame.Position.Y, size.X - _frame.End.X, _frame.Size.Y), Colors.Black);
        }

        float flash = (_elapsed - VanishAt) / FlashSeconds;
        if (flash is >= 0f and < 1f)
            DrawRect(new Rect2(Vector2.Zero, size), new Color(1f, 1f, 1f, 1f - flash));
    }

    private Sprite2D AddSprite(string texture, float scale = ArtScale)
    {
        var sprite = new Sprite2D
        {
            Texture = GD.Load<Texture2D>(VfxDirectory + texture + ".tres"),
            Scale = Vector2.One * scale,
            Visible = false,
        };
        this.AddChildSafely(sprite);
        return sprite;
    }

    private void AddBadge(string texture, float y)
    {
        var sprite = AddSprite(texture, BadgeScale);
        var origin = _center + BadgeAnchor + new Vector2(0f, y);
        sprite.Position = origin;
        _badges.Add((sprite, origin));
    }

    private void CheckPhase()
    {
        if (!_focusStarted && _elapsed >= FocusSeconds)
        {
            _focusStarted = true;
            // 读牌期间目标可能动过，合焦时重采样
            SampleTarget();
            FocusInSfx.Play(volumeMult: 1f);
        }

        if (!_vanished && _elapsed >= VanishAt)
        {
            _vanished = true;
            ShutterSfx.Play(volumeMult: 1f);
            _closed.TrySetResult();
        }

        if (_badgeStart >= 0f && _elapsed - _badgeStart >= BadgeTotalSeconds) Finish();
    }

    private void Finish()
    {
        _done = true;
        _finished.TrySetResult();
        this.QueueFreeSafely();
    }

    private void SampleTarget()
    {
        if (!GodotObject.IsInstanceValid(_target)) return;

        _center = ToLocal(_target.VfxSpawnPosition);
        _bounds = ToLocal(_target.Hitbox.GetGlobalRect());
    }

    private Vector2 ToLocal(Vector2 global) => GetGlobalTransform().AffineInverse() * global;

    private Rect2 ToLocal(Rect2 global) => new(ToLocal(global.Position), global.Size);

    // 绕圈游走：角度持续递增，不会变成单向平移后被半径限制卡住
    private void UpdateWobble(float delta, float focus)
    {
        if (focus >= 1f)
        {
            _wobbleOffset = Vector2.Zero;
            return;
        }

        _wobbleAngle += (WobbleAngleSpeed + _rng.RandfRange(-1f, 1f) * WobbleAngleJitter) * delta;
        _wobbleRadius = Mathf.Clamp(
            _wobbleRadius + _rng.RandfRange(-1f, 1f) * WobbleRadiusJitter * delta,
            WobbleRadius * 0.5f,
            WobbleRadius * 1.4f);
        _wobbleOffset = Vector2.Right.Rotated(_wobbleAngle) * _wobbleRadius;
    }

    private void UpdateVisuals(float delta)
    {
        float focus = Mathf.Clamp(_elapsed / FocusSeconds, 0f, 1f);
        float frameT = Mathf.Clamp((_elapsed - FocusSeconds) / FrameSeconds, 0f, 1f);
        bool framed = _focusStarted && !_vanished;

        // 准星：0.25s 淡入，1s 内恒速游走并随合焦收敛到 centerpos
        UpdateWobble(delta, focus);
        _crosshair.Visible = !_vanished;
        _crosshair.Position = _center + _wobbleOffset * (1f - focus);
        _crosshair.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(_elapsed / CrosshairFadeSeconds, 0f, 1f) * (1f - frameT));

        _frame = ComputeFrame(frameT);

        // Sprite2D 以中心为锚点，角标要按半尺寸向内缩才会让内角咬住框边
        var half = _corners[0].Texture.GetSize() * (CornerScale * 0.5f);
        _corners[2].Position = _frame.Position + half;
        _corners[3].Position = new Vector2(_frame.End.X - half.X, _frame.Position.Y + half.Y);
        _corners[0].Position = new Vector2(_frame.Position.X + half.X, _frame.End.Y - half.Y);
        _corners[1].Position = _frame.End - half;

        foreach (Sprite2D sprite in _corners) sprite.Visible = framed;

        // 大圆自适应：直径取相框长边
        _circle.Position = _frame.GetCenter();
        _circle.Scale = Vector2.One * (Mathf.Max(_frame.Size.X, _frame.Size.Y) / _circle.Texture.GetWidth());
        _circle.Visible = framed;

        UpdateBadges();
    }

    /// <summary>长宽各自不小于角标的两倍，否则同一侧的角会压在一起。</summary>
    private Rect2 ComputeFrame(float frameT)
    {
        Vector2 cornerSize = _corners[0].Texture.GetSize() * CornerScale;
        Vector2 size = new(Mathf.Max(_bounds.Size.X, cornerSize.X * 2f), Mathf.Max(_bounds.Size.Y, cornerSize.Y * 2f));
        Vector2 pos = _bounds.GetCenter() - size * 0.5f + new Vector2(0f, FrameDrop * (1f - frameT));
        return new Rect2(pos, size);
    }

    private void UpdateBadges()
    {
        if (_badgeStart < 0f) return;

        float t = _elapsed - _badgeStart;
        float pop = Mathf.Clamp(t / BadgePopSeconds, 0f, 1f);
        float fly = Mathf.Clamp((t - BadgePopSeconds - BadgeHoldSeconds) / BadgeFlySeconds, 0f, 1f);

        foreach ((Sprite2D sprite, Vector2 origin) in _badges)
        {
            sprite.Visible = true;
            sprite.Position = origin - new Vector2(0f, BadgeFlyDistance * fly * fly);
            sprite.Modulate = new Color(1f, 1f, 1f, pop * (1f - fly));
        }
    }
}
