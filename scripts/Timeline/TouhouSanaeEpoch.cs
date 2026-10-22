using MegaCrit.Sts2.Core.Timeline;

namespace TouhouAncients.Scripts.Timeline;

[TouhouTimelineStory("touhou_ancients_sanae_story")]
public sealed class TouhouSanaeStory : TouhouAncientTimelineStory
{
    protected override string StoryKey => "touhou_ancients_sanae_story";
}

[TouhouTimelineEpoch(TouhouTimelineColumns.Invitation8, Position = 3, EraSlug = TouhouTimelineColumns.Invitation8NameKey, StoryKey = "touhou_ancients_sanae_story", ChapterOrder = 0)]
public sealed class TouhouSanaeEpoch : TouhouAncientTimeline
{
    protected override string TimelineId => "TOUHOUANCIENTS_SANAE";

    protected override string? PortraitPath => "res://images/epoch/sanae.png";
}

internal static class TouhouTimelineColumns
{
    // Invitation7(2739) 之后的两列；均复用门扉的名称与年份
    public const int Invitation8 = 2740;
    public const int Invitation9 = 2741;
    public const string Invitation8NameKey = "INVITATION0";
    public const string Invitation9NameKey = "INVITATION0";
}
