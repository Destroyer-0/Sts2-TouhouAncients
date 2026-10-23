using MegaCrit.Sts2.Core.Timeline;

namespace TouhouAncients.Scripts.Timeline;

[TouhouTimelineEpoch(TouhouTimelineColumns.Invitation8, Position = 3, EraSlug = TouhouTimelineColumns.Invitation8NameKey, StoryKey = "touhou_ancients_story", ChapterOrder = 0)]
public sealed class TouhouSanaeEpoch : TouhouAncientTimeline
{
    protected override string TimelineId => "TOUHOUANCIENTS_SANAE";

    protected override string? PortraitPath => "res://images/epoch/sanae.png";
}
