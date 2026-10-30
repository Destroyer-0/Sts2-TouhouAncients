using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.addons.mega_text;
using TouhouAncients.Scripts.Hooks;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.Ui;

/// <summary>
/// 应援棒：本地玩家角色头顶的气氛读数。挂在 NCreature 下，随战斗节点一起销毁。
/// </summary>
public partial class AtmosphereLabel : Control
{
    private const string NodeName = "TouhouAtmosphereLabel";
    private const string LocTable = "gameplay_ui";
    private const string LocKey = "TOUHOUANCIENTS-ATMOSPHERE_LABEL";
    private const string FontPath = "res://themes/kreon_bold_glyph_space_two.tres";
    private const int FontSize = 28;

    /// <summary>留出头顶意图图标（半高 20）的位置，避免多人时压住意图。</summary>
    private const float GapAboveHead = 26f;

    private static readonly Vector2 LabelSize = new(360f, 44f);
    private static readonly Color OutlineColor = new(0.12f, 0.09f, 0.16f, 0.9f);
    private static readonly Color ShadowColor = new(0f, 0f, 0f, 0.5f);

    private MegaRichTextLabel _label = null!;
    private Player _player = null!;
    private Creature _creature = null!;
    private Control _anchor = null!;
    private int _shownAtmosphere = int.MinValue;
    private bool _shownHyped;

    /// <summary>在本地玩家的角色节点上挂读数据；已存在或无角色节点时什么都不做。</summary>
    public static void Create(Player player)
    {
        if (player.Creature is not { } creature) return;

        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (creatureNode == null) return;
        if (creatureNode.GetNodeOrNull<AtmosphereLabel>(NodeName) != null) return;

        AtmosphereLabel label = new() { Name = NodeName, MouseFilter = MouseFilterEnum.Ignore };
        label._player = player;
        label._creature = creature;
        label._anchor = creatureNode.IntentContainer;
        creatureNode.AddChildSafely(label);
    }

    public override void _Ready()
    {
        Size = LabelSize;
        MouseFilter = MouseFilterEnum.Ignore;

        _label = new MegaRichTextLabel
        {
            Name = "Text",
            MouseFilter = MouseFilterEnum.Ignore,
            BbcodeEnabled = true,
            AutoSizeEnabled = false,
            ScrollActive = false,
            ClipContents = false,
            AutowrapMode = TextServer.AutowrapMode.Off,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        // MegaRichTextLabel._Ready 没有 normal_font override 会直接抛异常
        Font font = GD.Load<Font>(FontPath);
        _label.AddThemeFontOverride(ThemeConstants.RichTextLabel.NormalFont, font);
        _label.AddThemeFontOverride(ThemeConstants.RichTextLabel.BoldFont, font);
        _label.AddThemeColorOverride(ThemeConstants.RichTextLabel.DefaultColor, StsColors.cream);
        _label.AddThemeColorOverride(ThemeConstants.RichTextLabel.FontOutlineColor, OutlineColor);
        _label.AddThemeColorOverride(ThemeConstants.RichTextLabel.FontShadowColor, ShadowColor);
        _label.AddThemeConstantOverride("outline_size", 6);
        _label.AddThemeConstantOverride("shadow_offset_x", 2);
        _label.AddThemeConstantOverride("shadow_offset_y", 2);
        foreach (StringName sizeKey in ThemeConstants.RichTextLabel.AllFontSizes)
        {
            _label.AddThemeFontSizeOverride(sizeKey, FontSize);
        }

        _label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        AddChild(_label);
        // 必须在 AddChild 之后：MegaRichTextLabel._Ready 会整体重置 CustomEffects
        _label.InstallEffect(new RainbowRichTextEffect());

        _creature.PowerApplied += OnPowerChanged;
        _creature.PowerRemoved += OnPowerChanged;
        _creature.PowerIncreased += OnPowerIncreased;
        _creature.PowerDecreased += OnPowerDecreased;
        CombatManager.Instance.CombatEnded += OnCombatEnded;

        Refresh();
        UpdatePosition();
    }

    public override void _ExitTree()
    {
        _creature.PowerApplied -= OnPowerChanged;
        _creature.PowerRemoved -= OnPowerChanged;
        _creature.PowerIncreased -= OnPowerIncreased;
        _creature.PowerDecreased -= OnPowerDecreased;
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_anchor)) return;

        if (_creature.IsDead)
        {
            Visible = false;
            return;
        }
        Visible = true;

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (!IsInstanceValid(_anchor)) return;

        // IntentContainer.Position 已扣掉自身半尺寸，本地中心点就是头顶的意图锚点
        Transform2D xform = _anchor.GetGlobalTransform();
        Vector2 center = xform * (_anchor.Size * 0.5f);
        Vector2 scale = xform.Scale;
        GlobalPosition = center - new Vector2(LabelSize.X * 0.5f * scale.X, LabelSize.Y * scale.Y + GapAboveHead);
    }

    private void OnPowerChanged(PowerModel power) => RefreshIfRelevant(power);

    private void OnPowerIncreased(PowerModel power, int change, bool silent) => RefreshIfRelevant(power);

    private void OnPowerDecreased(PowerModel power, bool silent) => RefreshIfRelevant(power);

    private void RefreshIfRelevant(PowerModel power)
    {
        if (power is AtmospherePower or HypedPower) Refresh();
    }

    private void Refresh()
    {
        int atmosphere = KoishiPerformanceCmd.GetAtmosphere(_player);
        bool hyped = KoishiPerformanceCmd.IsHyped(_player);
        if (atmosphere == _shownAtmosphere && hyped == _shownHyped) return;

        _shownAtmosphere = atmosphere;
        _shownHyped = hyped;

        string text = FormatAtmosphere(atmosphere);
        // 嗨到爆整段换色 + 抖动
        _label.Text = hyped ? $"[rainbow][jitter]{text}[/jitter][/rainbow]" : text;
    }

    private static string FormatAtmosphere(int atmosphere)
    {
        LocString? loc = LocString.GetIfExists(LocTable, LocKey);
        if (loc == null) return atmosphere.ToString();

        loc.Add("Atmosphere", atmosphere);
        return loc.GetFormattedText();
    }

    private void OnCombatEnded(CombatRoom room) => QueueFree();
}
