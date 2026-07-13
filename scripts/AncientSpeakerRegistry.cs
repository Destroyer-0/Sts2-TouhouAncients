using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Ancients;

namespace TouhouAncients.Scripts;

/// <summary>
/// 管理 Ancient 多说话者的配置和运行时映射。
/// </summary>
public static class AncientSpeakerRegistry
{
    private static readonly Dictionary<string, Dictionary<string, AncientSpeakerProfile>> _profiles = new();
    private static readonly ConditionalWeakTable<AncientDialogueLine, string> _lineVariants = new();

    /// <summary>
    /// 注册某个 Ancient 的多说话者配置。
    /// </summary>
    /// <param name="ancientLocPrefix">Ancient 的 loc 前缀，如 "TOUHOUANCIENTS-YORIGAMI_SISTER_ANCIENT"</param>
    /// <param name="profiles">变体ID → 头像/颜色配置</param>
    public static void RegisterProfiles(string ancientLocPrefix, Dictionary<string, AncientSpeakerProfile> profiles)
    {
        _profiles[ancientLocPrefix] = profiles;
    }

    /// <summary>
    /// 查找某个 Ancient 的特定变体配置。
    /// </summary>
    public static bool TryGetProfile(string ancientLocPrefix, string variantId, out AncientSpeakerProfile profile)
    {
        if (_profiles.TryGetValue(ancientLocPrefix, out var variants)
            && variants.TryGetValue(variantId, out profile))
            return true;
        profile = default!;
        return false;
    }

    /// <summary>
    /// 获取某个 Ancient 所有已注册的变体ID。
    /// </summary>
    public static IEnumerable<string> GetVariantIds(string ancientLocPrefix)
    {
        if (_profiles.TryGetValue(ancientLocPrefix, out var variants))
            return variants.Keys;
        return System.Array.Empty<string>();
    }

    /// <summary>
    /// 判断某个 Ancient 是否注册了多说话者配置。
    /// </summary>
    public static bool HasProfiles(string ancientLocPrefix)
    {
        return _profiles.ContainsKey(ancientLocPrefix);
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
