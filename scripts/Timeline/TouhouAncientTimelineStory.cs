using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Timeline;

namespace TouhouAncients.Scripts.Timeline;

/// <summary>Mod 故事基类；章节顺序由注册表决定，不再手写 Epochs。</summary>
public abstract class TouhouAncientTimelineStory : StoryModel
{
    protected abstract string StoryKey { get; }

    protected sealed override string Id => StringHelper.Slugify(StoryKey);

    public sealed override EpochModel[] Epochs => TouhouAncientTimelineRegistry.ResolveStoryEpochs(GetType());
}
