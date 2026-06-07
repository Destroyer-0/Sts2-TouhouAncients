using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;


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
    /// 确保 BannedTypeNames 至少从 BannedAncientData 同步过一次
    /// </summary>
    private static bool _bannedDataSynced;

    /// <summary>
    /// 扫描所有 AncientEventModel 子类（排除 Neow），仅填充类型列表。
    /// 同时主动从持久化数据同步禁用列表。
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

        // 主动从持久化数据同步禁用列表，确保后续 IsBanned 判断基于最新配置
        TouhouAncientsConfig.SyncBannedFromData();
        _bannedDataSynced = true;
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
    /// Prefix 拦截 ActModel.GenerateRooms：保存 rng 和 unlockState 供 Postfix 使用
    /// </summary>
    [HarmonyPatch]
    public static class ActModel_GenerateRooms_Patch
    {
        private static Rng _savedRng = null!;
        private static UnlockState _savedUnlockState = null!;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ActModel), nameof(ActModel.GenerateRooms));
        }

        [HarmonyPrefix]
        private static void Prefix(Rng rng, UnlockState unlockState, bool isMultiplayer)
        {
            _savedRng = rng;
            _savedUnlockState = unlockState;
        }

        [HarmonyPostfix]
        private static void Postfix(ActModel __instance)
        {
            //GD.Print("进入先古之民房间。");
            if (_savedRng == null || _savedUnlockState == null) return;

            var roomsField = AccessTools.Field(typeof(ActModel), "_rooms");
            if (roomsField?.GetValue(__instance) is not RoomSet rooms) return;

            AncientEventModel ancient;
            try { ancient = rooms.Ancient; } catch { return; }
            if (ancient == null) return;

            // GD.Print("先古之民：" + ancient.GetType().Name);
            // foreach (var bannedTypeName in TouhouAncientsConfig.BannedTypeNames)
            // {
            //     GD.Print("被禁用：" + bannedTypeName);
            // }
            if (IsBanned(ancient.GetType()))
            {
                var sharedSubsetField = AccessTools.Field(typeof(ActModel), "_sharedAncientSubset");
                var sharedSubset = sharedSubsetField?.GetValue(__instance) as List<AncientEventModel>;

                var pool = __instance.GetUnlockedAncients(_savedUnlockState)
                    .Concat(sharedSubset ?? new List<AncientEventModel>())
                    .Where(a => !IsBanned(a.GetType()))
                    .ToList();

                if (pool.Count > 0)
                {
                    rooms.Ancient = _savedRng.NextItem(pool)!;
                }
            }
        }
    }

    /// <summary>
    /// Prefix 拦截 CustomAncientModel.IsValidForAct(ActModel)
    /// </summary>
    [HarmonyPatch]
    public static class CustomAncientModel_IsValidForAct_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(CustomAncientModel), nameof(CustomAncientModel.IsValidForAct));
        }

        [HarmonyPrefix]
        private static bool Prefix(CustomAncientModel __instance, ref bool __result)
        {
            if (IsBanned(__instance.GetType()))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Prefix 拦截 AbstractModel.ShouldAllowAncient(Player, AncientEventModel)
    /// </summary>
    [HarmonyPatch]
    public static class AbstractModel_ShouldAllowAncient_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(AbstractModel), nameof(AbstractModel.ShouldAllowAncient));
        }

        [HarmonyPrefix]
        private static bool Prefix(AbstractModel __instance, AncientEventModel ancient, ref bool __result)
        {
            if (IsBanned(ancient.GetType()))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    private static bool IsBanned(Type type)
    {
        // 惰性同步：确保至少从持久化数据同步过一次
        if (!_bannedDataSynced)
        {
            _bannedDataSynced = true;
            TouhouAncientsConfig.SyncBannedFromData();
        }

        return TouhouAncientsConfig.BannedTypeNames.Contains(type.FullName!);
    }
}
