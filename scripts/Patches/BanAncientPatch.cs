using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 统一拦截所有 Ancient（先古之民）的 IsValidForAct 和 ShouldAllowAncient，
/// 根据 Config 中动态存储的禁用列表决定是否禁用。
/// 
/// Initialize() 时扫描所有 AncientEventModel 非抽象子类（排除 Neow），
/// 动态注入 settings_ui 本地化条目供 Config 的 SetupConfigUI 使用。
/// GetAllAncientEntries() 返回所有扫描到的 (Type, 标题) 列表，供 Config 生成复选框。
/// 
/// 添加新 Ancient 时：Config 中无需任何额外代码，Patches 和 UI 均自动适配。
/// </summary>
public static class BanAncientPatch
{
    private static readonly List<(Type type, string? title)> AllEntries = new();

    /// <summary>
    /// 扫描所有 AncientEventModel 子类（排除 Neow），仅填充类型列表。
    /// 不依赖 LocManager，避免 Mod Init 时崩溃。
    /// </summary>
    public static void Initialize()
    {
        AllEntries.Clear();

        var ancientTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    return Array.Empty<Type>();
                }
            })
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AncientEventModel)))
            .Where(t => t != typeof(Neow) && t != typeof(DeprecatedAncientEvent));

        foreach (var type in ancientTypes)
        {
            AllEntries.Add((type, null!));
        }
    }

    /// <summary>
    /// 获取某 Ancient 类型的中文标题，延迟从本地化表读取。
    /// </summary>
    public static string GetTitle(Type type)
    {
        try
        {
            var ancientEntry = ModelDb.GetEntry(type);
            var table = LocManager.Instance.GetTable("ancients");
            return table.GetRawText(ancientEntry + ".title");
        }
        catch
        {
            return type.Name.EndsWith("Ancient") ? type.Name.Replace("Ancient", "") : type.Name;
        }
    }

    /// <summary>
    /// 获取所有扫描到的 Ancient (类型, 中文标题) 列表。
    /// 标题在调用时实时从本地化表读取。同时惰性注入 settings_ui 条目。
    /// </summary>
    public static IReadOnlyList<(Type type, string title)> GetAllAncientEntries()
    {
        EnsureLocEntriesInjected();

        var result = new List<(Type type, string title)>(AllEntries.Count);
        foreach (var (type, _) in AllEntries)
        {
            result.Add((type, GetTitle(type!)));
        }
        return result;
    }

    private static bool _locEntriesInjected;

    private static void EnsureLocEntriesInjected()
    {
        if (_locEntriesInjected) return;
        _locEntriesInjected = true;

        try
        {
            var settingsTable = LocManager.Instance.GetTable("settings_ui");
            var dynamicEntries = new Dictionary<string, string>();
            foreach (var (type, _) in AllEntries)
            {
                var title = GetTitle(type!);
                var locKey = $"TOUHOUANCIENTS-{StringHelper.Slugify("BAN_" + type.Name)}.title";
                dynamicEntries[locKey] = "禁用" + title;
            }
            if (dynamicEntries.Count > 0)
            {
                settingsTable.MergeWith(dynamicEntries);
            }
        }
        catch
        {
            // LocManager 尚未就绪，下次调用时重试
            _locEntriesInjected = false;
        }
    }

    /// <summary>
    /// Prefix 拦截 CustomAncientModel.IsValidForAct(ActModel)
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CustomAncientModel), nameof(CustomAncientModel.IsValidForAct))]
    private static bool Prefix_IsValidForAct(CustomAncientModel __instance, ref bool __result)
    {
        if (IsBanned(__instance.GetType()))
        {
            __result = false;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Prefix 拦截 AbstractModel.ShouldAllowAncient(Player, AncientEventModel)
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ShouldAllowAncient))]
    private static bool Prefix_ShouldAllowAncient(AbstractModel __instance, AncientEventModel ancient, ref bool __result)
    {
        if (IsBanned(ancient.GetType()))
        {
            __result = false;
            return false;
        }
        return true;
    }

    private static bool IsBanned(Type type)
    {
        // 惰性同步：如果运行时集合为空但持久化数据有内容，则从持久化数据同步
        if (TouhouAncientsConfig.BannedTypeNames.Count == 0
            && !string.IsNullOrEmpty(TouhouAncientsConfig.BannedAncientData))
        {
            TouhouAncientsConfig.SyncBannedFromData();
        }

        return TouhouAncientsConfig.BannedTypeNames.Contains(type.FullName!);
    }
}
