using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Timeline;

namespace TouhouAncients.Scripts.Timeline;


internal static class TouhouTimelineColumns
{
    // Invitation7(2739) 之后的两列；均复用门扉的名称与年份
    public const int Invitation8 = 2740;
    public const int Invitation9 = 2741;
    public const string Invitation8NameKey = "INVITATION0";
    public const string Invitation9NameKey = "INVITATION0";
}

[TouhouTimelineStory("touhou_ancients_story")]
public sealed class TouhouAncientStory : TouhouAncientTimelineStory
{
    protected override string StoryKey => "touhou_ancients_story";
}
/// <summary>Mod 历史节点基类；布局、注册、故事归属由注册表统一处理。</summary>
public abstract class TouhouAncientTimeline : EpochModel
{
    protected abstract string TimelineId { get; }

    public sealed override string Id => TimelineId;

    public sealed override EpochEra Era => TouhouAncientTimelineRegistry.ResolveEra(GetType());

    public sealed override int EraPosition => TouhouAncientTimelineRegistry.ResolvePosition(GetType());

    public override string? StoryId => TouhouAncientTimelineRegistry.ResolveStoryId(GetType());

    /// <summary>详情大图；null 走 images/timeline/epoch_portraits/{id}.png。</summary>
    protected virtual string? PortraitPath => null;

    /// <summary>时间线槽位图；null 走 atlas，再回退到 PortraitPath / 约定 PNG。</summary>
    protected virtual string? SlotPortraitPath => null;

    public override EpochModel[] GetTimelineExpansion()
        => TouhouAncientTimelineRegistry.ResolveExpansion(GetType());

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

    internal static string ConventionPortraitPath(string id)
        => ImageHelper.GetImagePath($"timeline/epoch_portraits/{id.ToLowerInvariant()}.png");

    internal static string AtlasPath(string id)
        => ImageHelper.GetImagePath($"atlases/epoch_atlas.sprites/{id.ToLowerInvariant()}.tres");
}
