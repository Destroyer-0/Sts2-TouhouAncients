using System;
using System.Linq;
using MegaCrit.Sts2.Core.Localization;

namespace TouhouAncients.Scripts;

/// <summary>
/// 按角色 ID 子串匹配本地化变体键。扫描 {prefix}{token}{suffix}，
/// 若 <c>Character.Id.Entry</c> 包含该 token（忽略大小写）则采用，更长 token 优先。
/// 例如妹红 ID 含 MOKOU 时，可命中 fight.mokou.title / loss.mokou。
/// 层级与默认键平行：fight.title → fight.mokou.title，loss → loss.mokou。
/// </summary>
public static class CharacterLocVariant
{
    public static string? FindKey(string table, string prefix, string suffix, string? characterEntry)
    {
        if (string.IsNullOrEmpty(characterEntry)) return null;
        suffix ??= "";

        return LocManager.Instance.GetTable(table)
            .GetLocStringsWithPrefix(prefix)
            .Select(loc => loc.LocEntryKey)
            .Where(key => suffix.Length == 0 || key.EndsWith(suffix, StringComparison.Ordinal))
            .Select(key =>
            {
                int tokenLength = key.Length - prefix.Length - suffix.Length;
                return (
                    key,
                    token: tokenLength > 0 ? key.Substring(prefix.Length, tokenLength) : "");
            })
            .Where(x => x.token.Length > 0
                        && !x.token.Contains('.')
                        && characterEntry.Contains(x.token, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.token.Length)
            .Select(x => x.key)
            .FirstOrDefault();
    }

    public static LocString? Find(string table, string prefix, string suffix, string? characterEntry)
    {
        string? key = FindKey(table, prefix, suffix, characterEntry);
        return key == null ? null : new LocString(table, key);
    }
}
