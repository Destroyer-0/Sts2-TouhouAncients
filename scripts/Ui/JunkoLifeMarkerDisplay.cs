using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.Ui;

/// <summary>
/// 绀珠悯悼的生命标记：挂在血量显示节点（NCreatureStateDisplay）下，
/// 与血条共用淡入淡出/远程玩家选中显隐，位置对齐血条上沿。
/// </summary>
public partial class JunkoLifeMarkerDisplay : Control
{
    private const string NodeName = "TouhouJunkoLifeMarkers";
    private const string ScenePath = "res://images/sprite/junko/life_markers.tscn";
    private const float IconGap = 2f;
    private const float GapAboveBar = 4f;

    /// <summary>disable 覆盖层在「刚好盖住标记」基础上的额外放大倍数。</summary>
    private const float DisableScale = 2f;

    private readonly List<TextureRect> _icons = new();
    private TextureRect? _iconTemplate;
    private TextureRect? _disableIcon;
    private Texture2D? _fullTexture;
    private Texture2D? _emptyTexture;
    private Control? _hpBarContainer;
    private Vector2 _iconSize;
    private int _slots;
    private int _remaining;

    private float TotalWidth => _slots > 0 ? _slots * _iconSize.X + (_slots - 1) * IconGap : 0f;

    public static void Attach(Creature creature, int amount)
    {
        if (amount <= 0) return;
        NCreatureStateDisplay? display = ResolveStateDisplay(creature);
        if (display == null) return;

        if (display.GetNodeOrNull<JunkoLifeMarkerDisplay>(NodeName) is { } existing)
        {
            existing.SetRemaining(amount);
            return;
        }

        PackedScene? scene = PreloadManager.Cache.GetScene(ScenePath);
        JunkoLifeMarkerDisplay? marker = scene?.Instantiate<JunkoLifeMarkerDisplay>(PackedScene.GenEditState.Disabled);
        if (marker == null)
        {
            Log.Warn($"[TouhouAncients] 生命标记场景加载失败：{ScenePath}");
            return;
        }

        marker.Name = NodeName;
        marker._slots = amount;
        marker._remaining = amount;
        display.AddChild(marker);
    }

    public static void Sync(Creature creature, int remaining)
    {
        NCreatureStateDisplay? display = ResolveStateDisplay(creature);
        if (display?.GetNodeOrNull<JunkoLifeMarkerDisplay>(NodeName) is not { } marker) return;
        marker.SetRemaining(remaining);
    }

    public static void Detach(Creature creature)
    {
        NCreatureStateDisplay? display = ResolveStateDisplay(creature);
        if (display?.GetNodeOrNull<JunkoLifeMarkerDisplay>(NodeName) is not { } marker) return;
        // 先摘出树，避免同帧重新施加时被误判为已有节点
        marker.GetParent()?.RemoveChild(marker);
        marker.QueueFree();
    }

    private static NCreatureStateDisplay? ResolveStateDisplay(Creature creature) =>
        NCombatRoom.Instance?.GetCreatureNode(creature)?.GetNodeOrNull<NCreatureStateDisplay>("%HealthBar");

    public override void _Ready()
    {
        _iconTemplate = GetNodeOrNull<TextureRect>("%IconFull");
        TextureRect? emptyTemplate = GetNodeOrNull<TextureRect>("%IconEmpty");
        _disableIcon = GetNodeOrNull<TextureRect>("%Disable");
        _fullTexture = _iconTemplate?.Texture;
        _emptyTexture = emptyTemplate?.Texture;
        if (_iconTemplate != null) _iconSize = _iconTemplate.Size;

        NCreatureStateDisplay? display = GetParent() as NCreatureStateDisplay;
        _hpBarContainer = display?.GetNodeOrNull<NHealthBar>("%HealthBar")?.HpBarContainer;

        if (_iconTemplate == null || _fullTexture == null || _emptyTexture == null || _hpBarContainer == null)
        {
            QueueFree();
            return;
        }

        _slots = Math.Max(_slots, _remaining);
        RebuildIcons();
        UpdateDisableOverlay();

        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_hpBarContainer))
        {
            QueueFree();
            return;
        }

        Size = new Vector2(TotalWidth, _iconSize.Y);
        Vector2 barPos = _hpBarContainer!.GlobalPosition;
        Vector2 barSize = _hpBarContainer.Size;
        GlobalPosition = new Vector2(barPos.X + (barSize.X - Size.X) * 0.5f, barPos.Y - GapAboveBar - _iconSize.Y);
    }

    public void SetRemaining(int remaining)
    {
        _remaining = Math.Max(0, remaining);
        if (!IsNodeReady())
        {
            _slots = Math.Max(_slots, _remaining);
            return;
        }

        if (_remaining > _slots)
        {
            _slots = _remaining;
            RebuildIcons();
        }

        RefreshIcons();
        UpdateDisableOverlay();
    }

    private void RebuildIcons()
    {
        while (_icons.Count < _slots && _iconTemplate != null)
        {
            // 以场景里的模板复制，视觉参数统一放在 tscn
            TextureRect icon = (TextureRect)_iconTemplate.Duplicate();
            icon.Name = $"Life{_icons.Count}";
            icon.UniqueNameInOwner = false;
            icon.Visible = true;
            icon.Position = new Vector2(_icons.Count * (_iconSize.X + IconGap), 0f);
            AddChild(icon);
            _icons.Add(icon);
        }

        // 用树内顺序保证压在图标之上；z_index 会盖过全局滤镜与 UI 层
        if (_disableIcon != null) MoveChild(_disableIcon, GetChildCount() - 1);

        RefreshIcons();
    }

    private void RefreshIcons()
    {
        for (int i = 0; i < _icons.Count; i++)
        {
            _icons[i].Texture = i < _remaining ? _fullTexture : _emptyTexture;
        }
    }

    private void UpdateDisableOverlay()
    {
        if (_disableIcon == null) return;

        bool show = _remaining <= 0 && _icons.Count > 0 && _disableIcon.Texture != null;
        if (!show)
        {
            _disableIcon.Visible = false;
            return;
        }

        // 先等比缩放到刚好盖住整组标记，再整体放大；z_index 在 tscn 里置 1 保证盖在图标上
        Vector2 texSize = _disableIcon.Texture!.GetSize();
        float cover = Math.Max(TotalWidth / texSize.X, _iconSize.Y / texSize.Y);
        Vector2 overlaySize = texSize * cover * DisableScale;
        _disableIcon.Size = overlaySize;
        _disableIcon.Position = new Vector2((TotalWidth - overlaySize.X) * 0.5f, (_iconSize.Y - overlaySize.Y) * 0.5f);
        _disableIcon.Visible = true;
    }

    private void OnCombatEnded(CombatRoom room) => QueueFree();
}
