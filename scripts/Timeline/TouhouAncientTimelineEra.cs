using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Timeline;

namespace TouhouAncients.Scripts.Timeline;

/// <summary>时间线时代列：追加到原版列，或自建独立列。</summary>
public sealed class TouhouAncientTimelineEra
{
    private TouhouAncientTimelineEra(EpochEra value, string slug, string? iconPath, bool isCustom)
    {
        Value = value;
        Slug = slug;
        IconPath = iconPath;
        IsCustom = isCustom;
    }

    public EpochEra Value { get; }

    /// <summary>eras 表的键前缀，列名读 {Slug}.name、年份读 {Slug}.year。</summary>
    public string Slug { get; }

    public string? IconPath { get; }

    public bool IsCustom { get; }

    public static TouhouAncientTimelineEra AppendTo(EpochEra era)
        => new(era, StringHelper.Slugify(era.ToString()), null, false);

    public static TouhouAncientTimelineEra Custom(int eraValue, string slug, string? iconPath = null)
    {
        if (Enum.IsDefined(typeof(EpochEra), eraValue))
        {
            throw new ArgumentException("该整数已被原版时代占用；要追加到原版列请用 AppendTo。", nameof(eraValue));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("时代 slug 不能为空。", nameof(slug));
        }

        return new TouhouAncientTimelineEra((EpochEra)eraValue, StringHelper.Slugify(slug), iconPath, true);
    }
}
