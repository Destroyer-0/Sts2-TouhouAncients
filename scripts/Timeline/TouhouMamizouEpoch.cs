using MegaCrit.Sts2.Core.Timeline;

namespace TouhouAncients.Scripts.Timeline;

// Root：与早苗并列，谁先遇到谁先解锁
[TouhouTimelineEpoch(TouhouTimelineColumns.Invitation9, Position = 2, EraSlug = TouhouTimelineColumns.Invitation9NameKey, StoryKey = "touhou_ancients_story", ChapterOrder = 3, Root = true)]
public sealed class TouhouMamizouEpoch : TouhouAncientTimeline
{
    protected override string TimelineId => "TOUHOUANCIENTS_MAMIZOU";

    protected override string? PortraitPath => "res://images/epoch/mamizou.png";
}
