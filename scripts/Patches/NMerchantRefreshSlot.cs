using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.Patches;

public partial class NMerchantRefreshSlot : NMerchantSlot
{
    private Sprite2D _refreshVisual = null!;
    private Sprite2D _refreshVisualOutline = null!;
    private Control _costContainer = null!;
    private MerchantRefreshEntry _refreshEntry = null!;
    private bool _isUnavailable;

    public override MerchantEntry Entry => _refreshEntry;

    protected override CanvasItem Visual => _refreshVisual;
    protected Sprite2D Outline => _refreshVisualOutline;

    public static NMerchantRefreshSlot CreateFromCardRemovalTemplate(NMerchantCardRemoval template)
    {
        var slot = new NMerchantRefreshSlot
        {
            CustomMinimumSize = template.CustomMinimumSize,
            Size = template.Size,
            Scale = template.Scale,
            PivotOffset = template.PivotOffset,
            FocusMode = template.FocusMode,
            MouseFilter = template.MouseFilter
        };

        var templateVisual = template.GetNodeOrNull<Sprite2D>("%Visual");
        if (templateVisual != null)
        {
            var visual = templateVisual.Duplicate((int)Node.DuplicateFlags.UseInstantiation);
            visual.Name = "Visual";
            visual.UniqueNameInOwner = true;
            slot.AddChild(visual);
            AssignOwnerRecursive(visual, slot);
        }

        var templateCost = template.GetNodeOrNull<Control>("Cost");
        var templateCostLabel = template.FindChild("CostLabel", recursive: true, owned: false) as Label;

        var cost = new Control
        {
            Name = "Cost",
            Position = templateCost?.Position ?? new Vector2(0f, 65f),
            Size = templateCost?.Size ?? new Vector2(80f, 32f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        slot.AddChild(cost);
        cost.Owner = slot;

        var costLabel = new MegaLabel
        {
            Name = "CostLabel",
            UniqueNameInOwner = true,
            Size = cost.Size,
            MouseFilter = MouseFilterEnum.Ignore
        };
        cost.AddChild(costLabel);
        costLabel.Owner = slot;
        CopyLabelTheme(templateCostLabel, costLabel);

        var hitbox = new NClickableControl
        {
            Name = "Hitbox",
            UniqueNameInOwner = true,
            Size = template.Size,
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.None
        };
        slot.AddChild(hitbox);
        hitbox.Owner = slot;

        return slot;
    }

    public override void _Ready()
    {
        ConnectSignals();
        _refreshVisual = GetNode<Sprite2D>("%Visual");
        _refreshVisualOutline = _refreshVisual.GetChild(0) as Sprite2D;
        _costContainer = GetTemplateNode<Control>("Cost");
    }

    public void FillSlot(MerchantRefreshEntry refreshEntry)
    {
        if (_refreshEntry != null)
        {
            _refreshEntry.EntryUpdated -= UpdateVisual;
            _refreshEntry.PurchaseFailed -= base.OnPurchaseFailed;
            _refreshEntry.PurchaseCompleted -= OnSuccessfulPurchase;
        }

        // 保持图标原始大小，隐藏从模板继承的 outline
        var refreshTexture = ResourceLoader.Load<Texture2D>("res://images/ui/misc/tewi_refresh.png");
        _refreshVisual.Texture = refreshTexture;
        _refreshVisualOutline.Texture = refreshTexture;
        
        //_refreshVisual.Scale = refreshEntry.;
        _refreshVisual.Material = null;
        _hitbox.Size = refreshTexture.GetSize() * 0.5f;;

        _refreshEntry = refreshEntry;
        _refreshEntry.EntryUpdated += UpdateVisual;
        _refreshEntry.PurchaseFailed += base.OnPurchaseFailed;
        _refreshEntry.PurchaseCompleted += OnSuccessfulPurchase;
        UpdateVisual();
    }

    protected override void UpdateVisual()
    {
        base.UpdateVisual();
        if (_isUnavailable) return;

        if (!_refreshEntry.IsStocked)
        {
            var refreshTexture = ResourceLoader.Load<Texture2D>("res://images/ui/misc/tewi_refresh_02.png");
            _refreshVisual.Texture = refreshTexture;
            _refreshVisualOutline.Texture = refreshTexture;
            _hitbox.MouseFilter = MouseFilterEnum.Ignore;
            _isUnavailable = true;
            Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 0.45f);
            _costLabel.Visible = false;
            _costContainer.Visible = false;
            FocusMode = FocusModeEnum.None;
            ClearHoverTip();
            return;
        }

        var refreshTexture2 = ResourceLoader.Load<Texture2D>("res://images/ui/misc/tewi_refresh.png");
        _refreshVisual.Texture = refreshTexture2;
        _refreshVisualOutline.Texture = refreshTexture2;
        Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 1f);
        MouseFilter = MouseFilterEnum.Stop;
        _hitbox.MouseFilter = MouseFilterEnum.Stop;
        // 刷新按钮不需要价格标签
        _costLabel.Visible = false;
        _costContainer.Visible = false;
        FocusMode = FocusModeEnum.All;
        ClearHoverTip();
    }

    protected override async Task OnTryPurchase(MerchantInventory? inventory)
    {
        await _refreshEntry.OnTryPurchaseWrapper(inventory);
    }

    protected override void CreateHoverTip()
    {
        var hoverTipSet = NHoverTipSet.CreateAndShow(this, HoverTipFactory.FromRelic(_refreshEntry.Relic));
        if (hoverTipSet == null) return;

        hoverTipSet.GlobalPosition = GlobalPosition;
        if (GlobalPosition.X > GetViewport().GetVisibleRect().Size.X * 0.5f)
        {
            hoverTipSet.SetAlignment(this, HoverTipAlignment.Left);
            hoverTipSet.GlobalPosition -= Size * 0.5f * Scale;
        }
        else
        {
            hoverTipSet.SetAlignment(this, HoverTipAlignment.Right);
            hoverTipSet.GlobalPosition += Vector2.Right * Size.X * 0.5f * Scale + Vector2.Up * Size.Y * 0.5f * Scale;
        }
    }

    private void OnSuccessfulPurchase(PurchaseStatus _, MerchantEntry __)
    {
        TriggerMerchantHandToPointHere();
        UpdateVisual();
    }

    private T GetTemplateNode<T>(string name) where T : Node
    {
        return GetNodeOrNull<T>($"%{name}") ?? FindTemplateNode<T>(this, name)
            ?? throw new System.InvalidOperationException($"Merchant refresh slot is missing template node '{name}'.");
    }

    private static void AssignOwnerRecursive(Node node, Node owner)
    {
        node.Owner = owner;
        foreach (var child in node.GetChildren())
        {
            AssignOwnerRecursive(child, owner);
        }
    }

    private static T? FindTemplateNode<T>(Node root, string name) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child.Name == name && child is T typedChild)
            {
                return typedChild;
            }

            var descendant = FindTemplateNode<T>(child, name);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static void CopyLabelTheme(Label? source, MegaLabel target)
    {
        if (source == null)
        {
            return;
        }

        target.AddThemeFontOverride(ThemeConstants.Label.Font, source.GetThemeFont(ThemeConstants.Label.Font));
        target.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, source.GetThemeFontSize(ThemeConstants.Label.FontSize));
        target.AddThemeColorOverride(ThemeConstants.Label.FontColor, source.GetThemeColor(ThemeConstants.Label.FontColor));
        target.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, source.GetThemeColor(ThemeConstants.Label.FontOutlineColor));
        target.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize, source.GetThemeConstant(ThemeConstants.Label.OutlineSize));
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (_refreshEntry == null) return;

        _refreshEntry.EntryUpdated -= UpdateVisual;
        _refreshEntry.PurchaseFailed -= base.OnPurchaseFailed;
        _refreshEntry.PurchaseCompleted -= OnSuccessfulPurchase;
    }
}
