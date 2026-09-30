using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.TestSupport;

namespace TouhouAncients.Scripts.Rewards;

/// <summary>
/// 自定义奖励基类，仿照 RitsuLib 的 ModCustomReward，补齐图标与描述文本样板。
/// 子类仍需自备 [CustomEnum] 的 RewardType 字段、DeserializeMethod、MarkContentAsSeen、OnSelect。
/// 示例见 <see cref="UpgradeCardReward"/>。
/// </summary>
public abstract class TouhouCustomReward : CustomReward
{
    protected TouhouCustomReward(Player player)
        : base(player)
    {
    }

    /// <summary>奖励图标；null 则显示空白容器。本 mod 图片在 res://images/ 下，不带 mod 名前缀。</summary>
    protected virtual string? RewardIconPath => null;

    /// <summary>描述文本所在 LocTable，默认 gameplay_ui。</summary>
    protected virtual string DescriptionLocTable => "gameplay_ui";

    /// <summary>描述文本 Key，默认取 <see cref="DefaultDescriptionLocKey"/>。</summary>
    protected virtual string DescriptionLocKey => DefaultDescriptionLocKey;

    /// <summary>默认 Key：TOUHOUANCIENTS-&lt;类名蛇形大写&gt;，如 TOUHOUANCIENTS-UPGRADE_CARD_REWARD。</summary>
    protected string DefaultDescriptionLocKey => GetType().GetPrefix() + StringHelper.Slugify(GetType().Name);

    /// <summary>排序位，9 = 排在原版奖励（1~7）之后。</summary>
    public override int RewardsSetIndex => 9;

    /// <inheritdoc />
    public override bool IsPopulated => true;

    /// <inheritdoc />
    public override void Populate()
    {
    }

    /// <inheritdoc />
    protected override string? IconPath => RewardIconPath;

    /// <inheritdoc />
    public override LocString Description => LocalizedDescription();

    /// <summary>构造描述文本；变量对应 JSON 里的 {名}，如 ("cards", Amount) → {cards}。</summary>
    protected LocString LocalizedDescription(params (string Name, object? Value)[] variables)
    {
        var loc = new LocString(DescriptionLocTable, DescriptionLocKey);
        foreach (var (name, value) in variables)
        {
            switch (value)
            {
                case null:
                    break;
                case bool b:
                    loc.Add(name, b);
                    break;
                case string s:
                    loc.Add(name, s);
                    break;
                case LocString nested:
                    loc.Add(name, nested);
                    break;
                case IList<string> list:
                    loc.Add(name, list);
                    break;
                case decimal d:
                    loc.Add(name, d);
                    break;
                case int i:
                    loc.Add(name, (decimal)i);
                    break;
                default:
                    loc.AddObj(name, value);
                    break;
            }
        }
        return loc;
    }

    /// <summary>无图标时返回空 Control：调用方会直接访问返回值的 Position，返回 null 会空引用。</summary>
    public override Control? CreateIcon()
    {
        if (!string.IsNullOrEmpty(RewardIconPath))
        {
            return base.CreateIcon();
        }
        return TestMode.IsOn ? null : new Control();
    }
}
