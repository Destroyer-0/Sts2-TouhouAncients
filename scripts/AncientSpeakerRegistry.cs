using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts;

/// <summary>
/// 管理 Ancient 多说话者的配置和运行时映射。
/// 配置来源统一为 <see cref="TouhouAncientBase.SpeakerProfiles"/>（子类重写声明），
/// 首次查询时惰性解析并缓存，无需手动注册。
/// </summary>
public static class AncientSpeakerRegistry
{
    private static readonly Dictionary<string, IReadOnlyDictionary<string, AncientSpeakerProfile>> _profiles = new();
    private static readonly ConditionalWeakTable<AncientDialogueLine, string> _lineVariants = new();

    /// <summary>
    /// 惰性解析并缓存某个 Ancient 的多说话者配置。
    /// 首次查询时从 <see cref="MegaCrit.Sts2.Core.Models.ModelDb.AllAncients"/> 反查该
    /// Id.Entry 对应的 canonical 实例，读取其 <see cref="TouhouAncientBase.SpeakerProfiles"/>，
    /// 之后走缓存。
    /// </summary>
    private static IReadOnlyDictionary<string, AncientSpeakerProfile>? GetProfiles(string ancientEntry)
    {
        if (_profiles.TryGetValue(ancientEntry, out var cached))
            return cached;

        var ancient = ModelDb.AllAncients.FirstOrDefault(a => a.Id.Entry == ancientEntry);
        if (ancient is TouhouAncientBase { SpeakerProfiles: not null } touhouAncient)
        {
            _profiles[ancientEntry] = touhouAncient.SpeakerProfiles;
            return touhouAncient.SpeakerProfiles;
        }
        return null;
    }

    /// <summary>
    /// 查找某个 Ancient 的特定变体配置。
    /// </summary>
    public static bool TryGetProfile(string ancientEntry, string variantId, out AncientSpeakerProfile profile)
    {
        var profiles = GetProfiles(ancientEntry);
        if (profiles != null && profiles.TryGetValue(variantId, out var p))
        {
            profile = p;
            return true;
        }
        profile = default!;
        return false;
    }

    /// <summary>
    /// 获取某个 Ancient 所有已声明的变体ID。
    /// </summary>
    public static IEnumerable<string> GetVariantIds(string ancientEntry)
    {
        return GetProfiles(ancientEntry)?.Keys ?? Array.Empty<string>();
    }

    /// <summary>
    /// 判断某个 Ancient 是否声明了多说话者配置。
    /// </summary>
    public static bool HasProfiles(string ancientEntry)
    {
        return GetProfiles(ancientEntry) != null;
    }

    /// <summary>
    /// 将某条对话行关联到一个变体ID。
    /// </summary>
    public static void SetLineVariant(AncientDialogueLine line, string variantId)
    {
        _lineVariants.AddOrUpdate(line, variantId);
    }

    /// <summary>
    /// 尝试获取某条对话行的变体ID。
    /// </summary>
    public static bool TryGetLineVariant(AncientDialogueLine line, out string variantId)
    {
        return _lineVariants.TryGetValue(line, out variantId);
    }
}
