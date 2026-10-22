namespace TouhouAncients.Scripts.Timeline;

/// <summary>声明式注册故事；StoryKey 会 slugify 成 StoryModel.Id。</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TouhouTimelineStoryAttribute(string storyKey) : Attribute
{
    public string StoryKey { get; } = storyKey;
}

/// <summary>声明式注册历史节点；未注册的 StoryKey 会导致该节点不绑定章节。</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TouhouTimelineEpochAttribute(int eraValue) : Attribute
{
    public int EraValue { get; } = eraValue;

    /// <summary>-1 表示自动取该列最小空位。</summary>
    public int Position { get; init; } = -1;

    /// <summary>独立列必填；原版列忽略。</summary>
    public string? EraSlug { get; init; }

    public string? EraIconPath { get; init; }

    public string? StoryKey { get; init; }

    public int ChapterOrder { get; init; }

    /// <summary>true = 并列起点：不与上一章连成展开链，自己开根槽位。</summary>
    public bool Root { get; init; }
}
