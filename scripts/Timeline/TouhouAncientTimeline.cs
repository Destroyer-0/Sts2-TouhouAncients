using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;

namespace TouhouAncients.Scripts.Timeline;

/// <summary>本 Mod 时间线共用「门扉」列：复用原版名称与年份，排在 Invitation7 之后。</summary>
public sealed class TouhouAncientTimelineEra
{
    /// <summary>eras 表键前缀，名称与年份都复用原版门扉。</summary>
    public const string DoorNameKey = "INVITATION0";

    private TouhouAncientTimelineEra(EpochEra value)
    {
        Value = value;
    }

    public EpochEra Value { get; }

    /// <summary>第 column 个附加门扉列；0 紧接原版 Invitation7。</summary>
    public static TouhouAncientTimelineEra Door(int column = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        return new TouhouAncientTimelineEra((EpochEra)((int)EpochEra.Invitation7 + 1 + column));
    }
}

/// <summary>本 Mod 全部历史节点共用的唯一故事。</summary>
public sealed class TouhouAncientStory : TouhouAncientTimelineStory
{
    /// <summary>小写蛇形；原版会再 Slugify 一次，不能写全大写。</summary>
    public const string Key = "touhou_ancients_story";

    protected override string StoryKey => Key;
}

/// <summary>Mod 故事基类；章节顺序按时间线列与位置排列。</summary>
public abstract class TouhouAncientTimelineStory : StoryModel
{
    protected abstract string StoryKey { get; }

    protected sealed override string Id => StringHelper.Slugify(StoryKey);

    public sealed override EpochModel[] Epochs => TouhouAncientTimelineRegistry.ResolveStoryEpochs();
}

/// <summary>Mod 历史节点基类；布局与故事归属由注册表统一处理。</summary>
public abstract class TouhouAncientTimeline : EpochModel
{
    protected abstract string TimelineId { get; }

    public sealed override string Id => TimelineId;

    public sealed override EpochEra Era => TouhouAncientTimelineRegistry.ResolveEra(GetType());

    public sealed override int EraPosition => TouhouAncientTimelineRegistry.ResolvePosition(GetType());

    public override string? StoryId => TouhouAncientStory.Key;

    /// <summary>详情大图；null 走 images/timeline/epoch_portraits/{id}.png。</summary>
    protected virtual string? PortraitPath => null;

    /// <summary>时间线槽位图；null 走 atlas，再回退到 PortraitPath / 约定 PNG。</summary>
    protected virtual string? SlotPortraitPath => null;

    internal string ResolveSlotPortraitPath()
    {
        string explicitSlot = NormalizePath(SlotPortraitPath);
        if (explicitSlot.Length > 0)
        {
            return explicitSlot;
        }

        if (ResourceLoader.Exists(AtlasPath(Id)))
        {
            return string.Empty;
        }

        string portrait = NormalizePath(PortraitPath);
        if (portrait.Length > 0)
        {
            return portrait;
        }

        string convention = ConventionPortraitPath(Id);
        return ResourceLoader.Exists(convention) ? convention : string.Empty;
    }

    internal string ResolveDetailPortraitPath()
    {
        string portrait = NormalizePath(PortraitPath);
        if (portrait.Length > 0)
        {
            return portrait;
        }

        string convention = ConventionPortraitPath(Id);
        return ResourceLoader.Exists(convention) ? convention : string.Empty;
    }

    internal static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        path = path.Trim();
        return path.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            ? path
            : ImageHelper.GetImagePath(path.TrimStart('/'));
    }

    private static string ConventionPortraitPath(string id)
        => ImageHelper.GetImagePath($"timeline/epoch_portraits/{id.ToLowerInvariant()}.png");

    private static string AtlasPath(string id)
        => ImageHelper.GetImagePath($"atlases/epoch_atlas.sprites/{id.ToLowerInvariant()}.tres");
}

/// <summary>历史节点的解锁方式；只有默认、任意东方先古、指定东方先古三种。</summary>
public sealed class TouhouTimelineUnlock
{
    private TouhouTimelineUnlock(bool isDefault, Type? ancientType)
    {
        IsDefault = isDefault;
        AncientType = ancientType;
    }

    internal bool IsDefault { get; }

    private Type? AncientType { get; }

    /// <summary>开局即解锁。</summary>
    public static TouhouTimelineUnlock Default { get; } = new(true, null);

    /// <summary>遇到任意东方先古之民解锁。</summary>
    public static TouhouTimelineUnlock AnyTouhouAncient { get; } = new(false, null);

    /// <summary>遇到指定东方先古之民解锁。</summary>
    public static TouhouTimelineUnlock Ancient<TAncient>() where TAncient : TouhouAncientBase
        => new(false, typeof(TAncient));

    internal bool Matches(AncientEventModel ancient)
    {
        if (IsDefault)
        {
            return false;
        }

        return AncientType == null || AncientType.IsInstanceOfType(ancient);
    }
}
