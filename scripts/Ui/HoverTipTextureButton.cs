using System.Linq;
using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.Ui;

/// <summary>
/// 悬停时按本地化键弹出 STS 风格 HoverTip 的贴图按钮，运行期挂到事件 layout 上以固定在屏幕右下角。
/// 场景里只填键前缀，标题固定取 {前缀}.title、描述固定取 {前缀}.description，运行时可用 <see cref="SetLocKey"/> 替换。
/// 描述末尾固定追加一句设置提示，见 <see cref="HintKey"/>；填了 WorkshopId 则点击跳转创意工坊。
/// 同一个父节点下摆多个实例即为候选池：抽签只留一个，其余自删，所以在场景里加推荐只需多摆一个实例。
/// 显隐跟随 <see cref="TouhouAncientsConfig.EnableWorkshopRecommend"/>。
/// </summary>
[GlobalClass]
public partial class HoverTipTextureButton : TextureButton
{
    /// <summary>抽签结果的存放键；存在背景场景根节点上，随场景实例一起作废。</summary>
    private const string WinnerMetaKey = "touhouancients_recommend_winner";

    /// <summary>抽签种子的盐，与游戏其它 Rng 派生名错开。</summary>
    private const string SeedSalt = "TouhouAncientsRecommend:";
    /// <summary>固定追加的提示所在表/键；用常量而非本按钮的 LocTable，各按钮复用同一句。</summary>
    private const string HintTable = "gameplay_ui";

    private const string HintKey = "TOUHOUANCIENTS-RECOMMEND_HINT";

    private const string LocTable = "gameplay_ui";

    private const string TitleSuffix = ".title";

    private const string DescriptionSuffix = ".description";

    /// <summary>本地化键前缀（不含后缀）；标题固定拼 .title，描述固定拼 .description。</summary>
    [Export]
    public string LocKey { get; set; } = "";

    /// <summary>创意工坊条目 ID（纯数字）；填了则点击按钮跳到它的创意工坊页。</summary>
    [Export]
    public string WorkshopId { get; set; } = "";

    /// <summary>距屏幕右边距；位置由代码按这两个边距决定，见 <see cref="MoveToEventLayout"/>。</summary>
    [Export]
    public int MarginRight { get; set; } = 20;

    /// <summary>距屏幕下边距。</summary>
    [Export]
    public int MarginBottom { get; set; } = 96;

    private EventHandler? _onConfigChanged;

    private Action? _onConfigReloaded;

    /// <summary>放在 _EnterTree 而非 _Ready：<see cref="Reparent"/> 会触发一次 Exit/Enter，_Ready 不会再跑第二次。</summary>
    public override void _EnterTree()
    {
        SubscribeConfigChanges();
    }

    public override void _ExitTree()
    {
        UnsubscribeConfigChanges();
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        MouseEntered += ShowTip;
        MouseExited += HideTip;
        Pressed += OnPressed;
        // 节点被隐藏时不会补发 MouseExited，不接这里 Tip 会残留在屏幕上
        Connect(CanvasItem.SignalName.VisibilityChanged, Callable.From(OnVisibilityChanged));
        WarnIfMouseBlocked();
        // 延迟回调前一帧就会渲染，先按开关定好显隐
        Visible = TouhouAncientsConfig.EnableWorkshopRecommend;
        MoveToEventLayout();
    }

    /// <summary>订阅 Mod 配置，让玩家在暂停菜单里改设置时立刻生效。</summary>
    private void SubscribeConfigChanges()
    {
        if (_onConfigChanged != null || ModConfigRegistry.Get<TouhouAncientsConfig>() is not { } config)
        {
            return;
        }

        // 直接挂实例方法（而非 BaseLib 的 _configChangedHandlers）：那份列表由 ClearUIEventHandlers 清，
        // 本按钮的生命周期与设置页无关，自己登记自己注销。
        _onConfigChanged = (_, _) => RefreshVisibility();
        _onConfigReloaded = RefreshVisibility;
        config.ConfigChanged += _onConfigChanged;
        config.OnConfigReloaded += _onConfigReloaded;
    }

    private void UnsubscribeConfigChanges()
    {
        if (ModConfigRegistry.Get<TouhouAncientsConfig>() is { } config)
        {
            if (_onConfigChanged != null)
            {
                config.ConfigChanged -= _onConfigChanged;
            }

            if (_onConfigReloaded != null)
            {
                config.OnConfigReloaded -= _onConfigReloaded;
            }
        }

        _onConfigChanged = null;
        _onConfigReloaded = null;
    }

    /// <summary>总开关只改显隐、不释放节点：玩家在暂停菜单里改完能原地恢复。</summary>
    private void RefreshVisibility()
    {
        bool enabled = TouhouAncientsConfig.EnableWorkshopRecommend;
        Visible = enabled;
        if (!enabled)
        {
            HideTip();
        }
    }

    private void OnPressed()
    {
        if (string.IsNullOrEmpty(WorkshopId))
        {
            return;
        }

        // 走 PlatformUtil：Steam 下用客户端覆盖层浏览器打开，非 Steam 退回系统默认浏览器
        PlatformUtil.OpenUrl($"https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopId}");
    }

    /// <summary>装饰子节点盖在按钮上会连悬停一起吃掉，只报不改（可能是有意为之的交互层）。</summary>
    private void WarnIfMouseBlocked()
    {
        foreach (Control child in GetChildren().OfType<Control>())
        {
            if (child.MouseFilter != MouseFilterEnum.Ignore)
            {
                Log.Warn($"[TouhouAncients] {Name}：子节点 {child.Name} 未设 mouse_filter = Ignore，会挡住本按钮的鼠标事件");
            }
        }
    }

    /// <summary>
    /// 把按钮钉到屏幕右下角：背景场景整体被 %AncientBgContainer 按宽高比缩放位移，在那里锚点不等于屏幕坐标，
    /// 只能挂到事件 layout（全屏 Control）上；追加在末尾顺带排在 %VfxContainer 之后，鼠标不会再被那层 VFX 拿走。
    /// 必须等父节点的 add_child 结束再做：<c>_Ready</c> 就跑在 add_child 里，此时引擎禁止 remove_child
    /// （日志只打 ERROR、不抛异常），直接 Reparent 会静默失败、按钮原地留在背景场景里。
    /// 挂过去后锚点一律重设（编辑器在 Position 模式下拖过这个实例的话，加载时锚点已被重置成左上角），
    /// 位置只认 <see cref="MarginRight"/> / <see cref="MarginBottom"/>。
    /// </summary>
    private void MoveToEventLayout()
    {
        Callable.From(MoveToEventLayoutDeferred).CallDeferred();
    }

    private void MoveToEventLayoutDeferred()
    {
        if (!GodotObject.IsInstanceValid(this))
        {
            return;
        }

        // 抽签必须在 Reparent 之前：挂到事件 layout 后，兄弟列表里就不是背景场景那批按钮了
        if (!TryWinLottery())
        {
            this.QueueFreeSafely();
            return;
        }

        RepositionToLayout();
        RefreshVisibility();
    }

    /// <summary>
    /// 同父节点的候选按钮抽签。首个跑到的实例在兄弟里随机挑一个赢家、写进父节点 meta，其余只读结果。
    /// 单个实例（候选池只有自己）时直接胜出，等于没抽签。
    /// </summary>
    private bool TryWinLottery()
    {
        Node? parent = GetParent();
        if (parent == null)
        {
            return true;
        }

        if (parent.HasMeta(WinnerMetaKey))
        {
            // 背景场景节点被复用时会读到上一场的旧赢家（早已释放）→ 失效即重抽
            if (parent.GetMeta(WinnerMetaKey).As<HoverTipTextureButton>() is { } stored &&
                GodotObject.IsInstanceValid(stored))
            {
                return ReferenceEquals(stored, this);
            }

            parent.RemoveMeta(WinnerMetaKey);
        }

        var candidates = parent.GetChildren().OfType<HoverTipTextureButton>().ToList();
        int index = candidates.Count > 0 ? PickCandidateIndex(candidates.Count) : 0;
        HoverTipTextureButton winner = candidates.Count > 0 ? candidates[index] : this;
        parent.SetMeta(WinnerMetaKey, winner);
        return ReferenceEquals(winner, this);
    }

    /// <summary>
    /// 用运行种子派生一个独立 Rng 决定人选，保证联机各端同一个人选。
    /// 必须用 <c>RunState.Rng.Seed</c>：<c>PlayerRng</c> 的种子含玩家槽位，各端本来就不同；
    /// 也不能掷 <c>EventModel.Rng</c>（非共享事件同样含槽位，且会挪动事件自己的序列）。
    /// 新建的 Rng 实例只用于这一次取数，任何随机队列都不消耗。
    /// </summary>
    private int PickCandidateIndex(int count)
    {
        try
        {
            // State 是私有属性，取不到；这条链全公开，也不需要在战斗里
            EventModel localEvent = RunManager.Instance.EventSynchronizer.GetLocalEvent();
            if (localEvent.Owner is { } owner)
            {
                // 混入事件 Id：同一局里不同先古之民抽到不同推荐
                return new Rng(owner.RunState.Rng.Seed, SeedSalt + localEvent.Id.Entry).NextInt(count);
            }
        }
        catch (Exception ex)
        {
            // 不在事件房里时同步器的 _events 为空，GetLocalEvent 会越界
            Log.Warn($"[TouhouAncients] {Name}：推荐抽签取种子失败，退回固定人选（{ex.Message}）");
        }

        return 0;
    }

    private void RepositionToLayout()
    {
        Node? ancestor = GetParent();
        while (ancestor != null && ancestor is not NEventLayout)
        {
            ancestor = ancestor.GetParent();
        }

        if (ancestor is not NEventLayout layout || !GodotObject.IsInstanceValid(layout) || GetParent() == layout)
        {
            return;
        }

        Reparent(layout, keepGlobalTransform: false);

        Vector2 size = Size.X > 1f && Size.Y > 1f ? Size : TextureNormal?.GetSize() ?? Size;
        SetAnchorsPreset(LayoutPreset.BottomRight);
        OffsetRight = -MarginRight;
        OffsetBottom = -MarginBottom;
        OffsetLeft = -MarginRight - size.X;
        OffsetTop = -MarginBottom - size.Y;

        Log.Info($"[TouhouAncients] {Name}：layout={layout.GetGlobalRect()} viewport={GetViewport()?.GetVisibleRect()} self={GetGlobalRect()}");
    }

    /// <summary>运行时换键；若 Tip 正在显示，需重新悬停才会刷新。</summary>
    public void SetLocKey(string locKey)
    {
        LocKey = locKey;
    }

    private void OnVisibilityChanged()
    {
        if (!IsVisibleInTree())
        {
            HideTip();
        }
    }

    private void ShowTip()
    {
        Texture2D? icon = TextureNormal;
        LocString? title = FindSuffixedLoc(LocTable, TitleSuffix);

        if (title == null)
        {
            // 无标题只能用 2 参构造，它整段吃 LocString、不接受拼好的字符串，末尾提示只能放弃
            LocString? descriptionLoc = FindSuffixedLoc(LocTable, DescriptionSuffix);
            if (descriptionLoc == null)
            {
                Log.Warn($"[TouhouAncients] {Name}：本地化键缺失（{LocTable}/{LocKey}{TitleSuffix}、{LocTable}/{LocKey}{DescriptionSuffix}），没有 HoverTip 可显示");
                return;
            }

            CreateTip(new HoverTip(descriptionLoc, icon));
            return;
        }

        string description = GetSuffixedText(LocTable, DescriptionSuffix);
        string hint = GetText(HintTable, HintKey);
        if (hint.Length > 0)
        {
            description = description.Length > 0 ? description + "\n" + hint : hint;
        }

        CreateTip(new HoverTip(title, description, icon));
    }

    private void CreateTip(HoverTip tip)
    {
        // alignment 不能传 None：那样 SetFollowOwner 会把 Tip 钉在原点（见 docs/项目参考.md 4.6.2）
        NHoverTipSet? set = NHoverTipSet.CreateAndShow(this, tip, HoverTip.GetHoverTipAlignment(this));
        set?.SetFollowOwner();
    }

    private void HideTip()
    {
        NHoverTipSet.Remove(this);
    }

    /// <summary>缺键返回 null：对不存在的键直接取文本会抛 LocException。</summary>
    private LocString? FindLocString(string table, string key)
    {
        return string.IsNullOrEmpty(key) ? null : LocString.GetIfExists(table, key);
    }

    private LocString? FindSuffixedLoc(string table, string suffix)
    {
        return string.IsNullOrEmpty(LocKey) ? null : FindLocString(table, LocKey + suffix);
    }

    private string GetText(string table, string key)
    {
        return FindLocString(table, key)?.GetFormattedText() ?? "";
    }

    private string GetSuffixedText(string table, string suffix)
    {
        return FindSuffixedLoc(table, suffix)?.GetFormattedText() ?? "";
    }
}