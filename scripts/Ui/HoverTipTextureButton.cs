using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Platform;

namespace TouhouAncients.Scripts.Ui;

/// <summary>
/// 悬停时按本地化键弹出 STS 风格 HoverTip 的贴图按钮。
/// 键（json 表名 + 条目键）在检视面板填，运行时可用 <see cref="SetLocKeys"/> 替换。
/// 描述末尾固定追加一句设置提示，见 <see cref="HintKey"/>；填了 WorkshopId 则点击跳转创意工坊。
/// </summary>
[GlobalClass]
public partial class HoverTipTextureButton : TextureButton
{
    /// <summary>固定追加的提示所在表/键；用常量而非本按钮的 LocTable，各按钮复用同一句。</summary>
    private const string HintTable = "gameplay_ui";

    private const string HintKey = "TOUHOUANCIENTS-RECOMMEND_HINT";

    /// <summary>本地化表名（json 文件名），如 gameplay_ui / badges。</summary>
    [Export]
    public string LocTable { get; set; } = "gameplay_ui";

    /// <summary>标题键。留空则是无标题 Tip，但那种写法拼不上末尾提示（原因见 ShowTip）。</summary>
    [Export]
    public string TitleKey { get; set; } = "";

    /// <summary>描述键。</summary>
    [Export]
    public string DescriptionKey { get; set; } = "";

    /// <summary>用按钮自身的 texture_normal 当 HoverTip 图标。</summary>
    [Export]
    public bool UseOwnTextureAsIcon { get; set; } = true;

    /// <summary>HoverTip 是否跟随本节点移动。</summary>
    [Export]
    public bool FollowOwner { get; set; } = true;

    /// <summary>创意工坊条目 ID（纯数字）；填了则点击按钮跳到它的创意工坊页。</summary>
    [Export]
    public string WorkshopId { get; set; } = "";

    public override void _Ready()
    {
        MouseEntered += ShowTip;
        MouseExited += HideTip;
        Pressed += OnPressed;
        // 节点被隐藏时不会补发 MouseExited，不接这里 Tip 会残留在屏幕上
        Connect(CanvasItem.SignalName.VisibilityChanged, Callable.From(OnVisibilityChanged));
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

    /// <summary>运行时换键；若 Tip 正在显示，需重新悬停才会刷新。</summary>
    public void SetLocKeys(string locTable, string titleKey, string descriptionKey)
    {
        LocTable = locTable;
        TitleKey = titleKey;
        DescriptionKey = descriptionKey;
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
        Texture2D? icon = UseOwnTextureAsIcon ? TextureNormal : null;
        LocString? title = FindLocString(LocTable, TitleKey);

        if (title == null)
        {
            // 无标题只能用 2 参构造，它整段吃 LocString、不接受拼好的字符串，末尾提示只能放弃
            LocString? descriptionLoc = FindLocString(LocTable, DescriptionKey);
            if (descriptionLoc == null)
            {
                return;
            }

            CreateTip(new HoverTip(descriptionLoc, icon));
            return;
        }

        string description = GetText(LocTable, DescriptionKey);
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
        if (FollowOwner)
        {
            set?.SetFollowOwner();
        }
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

    private string GetText(string table, string key)
    {
        return FindLocString(table, key)?.GetFormattedText() ?? "";
    }
}
