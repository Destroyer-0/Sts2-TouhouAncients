namespace TouhouAncients.Scripts.Timeline;

public sealed class TouhouSanaeEpoch : TouhouAncientTimeline
{
    protected override string TimelineId => "TOUHOUANCIENTS_SANAE";

    protected override string? PortraitPath => "res://images/epoch/sanae.png";
}

public sealed class TouhouMamizouEpoch : TouhouAncientTimeline
{
    protected override string TimelineId => "TOUHOUANCIENTS_MAMIZOU";

    protected override string? PortraitPath => "res://images/epoch/mamizou.png";
}

/// <summary>本 Mod 全部时间线节点；在 Initialize 之前调用一次 Register。</summary>
internal static class TouhouAncientTimelineContent
{
    public static void Register()
    {
        TouhouAncientTimelineRegistry.Register<TouhouSanaeEpoch>(
            TouhouAncientTimelineEra.Door(0),
            TouhouTimelineUnlock.Ancient<KotiyaSanaeAncient>(),
            position: 3);

        TouhouAncientTimelineRegistry.Register<TouhouMamizouEpoch>(
            TouhouAncientTimelineEra.Door(1),
            TouhouTimelineUnlock.Ancient<FutatsuiwaMamizouAncient>(),
            position: 2);
    }
}
