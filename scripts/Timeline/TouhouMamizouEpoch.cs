using MegaCrit.Sts2.Core.Timeline;

namespace TouhouAncients.Scripts.Timeline;

[TouhouTimelineEpoch(TouhouTimelineColumns.Invitation9, Position = 2, EraSlug = TouhouTimelineColumns.Invitation9NameKey)]
public sealed class TouhouMamizouEpoch : TouhouAncientTimeline
{
    protected override string TimelineId => "TOUHOUANCIENTS_MAMIZOU";

    protected override string? PortraitPath => "res://images/epoch/mamizou.png";
}
