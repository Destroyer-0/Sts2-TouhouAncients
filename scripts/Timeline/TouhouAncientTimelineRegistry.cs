using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;

namespace TouhouAncients.Scripts.Timeline;

/// <summary>本 Mod 时间线的注册、布局、槽位与解锁入口；在 Entry.Init 内调用一次 Initialize。</summary>
public static class TouhouAncientTimelineRegistry
{
    private const int MaxAutoPositionScan = 128;

    private static readonly object Sync = new();
    private static readonly List<Registration> Epochs = [];
    private static readonly Dictionary<Type, TouhouAncientTimelineEra> EraByType = [];
    private static readonly Dictionary<Type, int> PositionByType = [];
    private static readonly Dictionary<EpochEra, string> ColumnNameKeys = [];
    private static readonly HashSet<string> RegisteredIds = new(StringComparer.Ordinal);
    private static readonly HashSet<(EpochEra Era, int Position)> Occupied = [];

    private static bool _initialized;

    public static bool IsInitialized => _initialized;

    /// <summary>本 Mod 已注册的全部时间线节点 Id。</summary>
    public static IReadOnlySet<string> RegisteredEpochIds => RegisteredIds;

    /// <summary>注册节点；必须在 Initialize 之前调用。</summary>
    public static void Register<TEpoch>(TouhouAncientTimelineEra era, TouhouTimelineUnlock unlock, int? position = null)
        where TEpoch : TouhouAncientTimeline, new()
    {
        if (_initialized)
        {
            throw new InvalidOperationException("时间线注册已冻结；请在 Entry.Init 的 Initialize 之前注册。");
        }

        if (Epochs.Any(registration => registration.EpochType == typeof(TEpoch)))
        {
            Log.Warn($"[TouhouAncients] 节点重复注册已跳过：{typeof(TEpoch).Name}");
            return;
        }

        Epochs.Add(new Registration(typeof(TEpoch), era, unlock, position));
    }

    /// <summary>写入游戏字典并分配布局；重复调用无副作用。</summary>
    public static void Initialize()
    {
        lock (Sync)
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                RuntimeHelpers.RunClassConstructor(typeof(EpochModel).TypeHandle);
                RuntimeHelpers.RunClassConstructor(typeof(StoryModel).TypeHandle);
                SeedVanillaOccupancy();
                RegisterStoryInGame();
                RegisterEpochsInGame();
                RegisterEpochsInAllEpochs();
                AssignLayout();
                RefreshAllEpochIds();
                _initialized = true;
                Log.Info($"[TouhouAncients] Timeline registered: {Epochs.Count} epoch(s).");
            }
            catch (Exception ex)
            {
                Log.Error($"[TouhouAncients] Timeline registration failed: {ex}");
            }
        }
    }

    public static EpochEra ResolveEra(Type epochType)
        => EraByType.TryGetValue(epochType, out TouhouAncientTimelineEra? era)
            ? era.Value
            : throw new InvalidOperationException($"时间线节点 {epochType.Name} 未注册。");

    public static int ResolvePosition(Type epochType)
        => PositionByType.TryGetValue(epochType, out int position)
            ? position
            : throw new InvalidOperationException($"时间线节点 {epochType.Name} 未注册。");

    /// <summary>故事章节：按列与位置从下到上排列。</summary>
    public static EpochModel[] ResolveStoryEpochs()
        => Epochs
            .Select(registration => (registration.EpochType, Era: ResolveEra(registration.EpochType), Position: ResolvePosition(registration.EpochType)))
            .OrderBy(item => (int)item.Era)
            .ThenBy(item => item.Position)
            .Select(item => EpochModel.Get(EpochModel.GetId(item.EpochType)))
            .ToArray();

    /// <summary>本 Mod 自建列复用的 eras 键；原版列返回 false。</summary>
    public static bool TryGetColumnNameKey(EpochEra era, out string? nameKey)
        => ColumnNameKeys.TryGetValue(era, out nameKey);

    /// <summary>补齐槽位并把默认解锁的节点标记为已获得；幂等。</summary>
    public static void EnsureSlots() => EnsureSlots(SaveManager.Instance.Progress);

    public static void EnsureSlots(ProgressState? progress)
    {
        if (!_initialized || progress == null)
        {
            return;
        }

        foreach (Registration registration in Epochs)
        {
            string id = EpochModel.GetId(registration.EpochType);
            SerializableEpoch? saved = FindEpoch(progress, id);
            if (saved == null || saved.State == EpochState.None || saved.State == EpochState.ObtainedNoSlot)
            {
                progress.UnlockSlot(id);
            }

            if (registration.Unlock.IsDefault && (saved == null || saved.State < EpochState.Obtained))
            {
                progress.ObtainEpoch(id);
            }
        }
    }

    /// <summary>清空本 Mod 全部节点进度并恢复成首次安装状态；返回清掉的条目数。</summary>
    public static int ClearAllProgress(ProgressState? progress = null)
    {
        progress ??= SaveManager.Instance.Progress;
        if (!_initialized || progress == null)
        {
            return 0;
        }

        int removed = 0;
        if (progress.Epochs is List<SerializableEpoch> epochs)
        {
            // 直接删条目 = 首次安装状态；留 NotObtained 会让节点提前出现
            removed = epochs.RemoveAll(entry => RegisteredIds.Contains(entry.Id));
        }
        else
        {
            foreach (string id in RegisteredIds)
            {
                progress.ObtainEpochOverride(id, EpochState.NotObtained);
            }

            Log.Warn("[TouhouAncients] ProgressState.Epochs 不是 List，改用状态回退清除。");
        }

        EnsureSlots(progress);
        return removed;
    }

    /// <summary>遇到东方先古之民时按解锁方式开节点；多人只处理本机账号。</summary>
    internal static void EvaluateAncientEncounter(EventModel eventModel, Player owner)
    {
        if (!_initialized || eventModel is not AncientEventModel ancient || ancient is not TouhouAncientBase)
        {
            return;
        }

        if (owner == null || (LocalContext.NetId.HasValue && !LocalContext.IsMe(owner)))
        {
            return;
        }

        if (owner.RunState.GameMode.AreAchievementsAndEpochsLocked())
        {
            return;
        }

        EnsureSlots();
        foreach (Registration registration in Epochs)
        {
            if (registration.Unlock.Matches(ancient))
            {
                Unlock(registration.EpochType, owner.DiscoveredEpochs);
            }
        }
    }

    private static void Unlock(Type epochType, ICollection<string> discovered)
    {
        string id = EpochModel.GetId(epochType);
        SerializableEpoch? saved = FindEpoch(SaveManager.Instance.Progress, id);
        if (saved != null && saved.State >= EpochState.Obtained)
        {
            return;
        }

        SaveManager.Instance.ObtainEpoch(id);
        if (!discovered.Contains(id))
        {
            discovered.Add(id);
        }
    }

    private static SerializableEpoch? FindEpoch(ProgressState progress, string id)
        => progress.Epochs.FirstOrDefault(entry => entry.Id == id);

    private static void SeedVanillaOccupancy()
    {
        Dictionary<string, Type> epochTypes = GetStaticField<Dictionary<string, Type>>(typeof(EpochModel), "_epochTypeDictionary");
        foreach (Type type in epochTypes.Values.Distinct())
        {
            if (typeof(TouhouAncientTimeline).IsAssignableFrom(type))
            {
                continue;
            }

            EpochModel epoch = (EpochModel)Activator.CreateInstance(type)!;
            Occupied.Add((epoch.Era, epoch.EraPosition));
        }
    }

    private static void RegisterStoryInGame()
    {
        Dictionary<string, Type> storyTypes = GetStaticField<Dictionary<string, Type>>(typeof(StoryModel), "_storyTypeDictionary");
        storyTypes[StringHelper.Slugify(TouhouAncientStory.Key)] = typeof(TouhouAncientStory);
    }

    private static void RegisterEpochsInGame()
    {
        Dictionary<string, Type> epochTypes = GetStaticField<Dictionary<string, Type>>(typeof(EpochModel), "_epochTypeDictionary");
        Dictionary<Type, string> typeToId = GetStaticField<Dictionary<Type, string>>(typeof(EpochModel), "_typeToIdDictionary");

        foreach (Registration registration in Epochs)
        {
            TouhouAncientTimeline epoch = (TouhouAncientTimeline)Activator.CreateInstance(registration.EpochType)!;
            string id = epoch.Id;

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException($"节点 {registration.EpochType.Name} 的 Id 为空。");
            }

            if (epochTypes.TryGetValue(id, out Type? existing) && existing != registration.EpochType)
            {
                throw new InvalidOperationException($"节点 Id 冲突：{id} 已被 {existing.Name} 占用（{registration.EpochType.Name}）。");
            }

            if (!RegisteredIds.Add(id))
            {
                throw new InvalidOperationException($"节点 Id 重复注册：{id}。");
            }

            epochTypes[id] = registration.EpochType;
            typeToId[registration.EpochType] = id;
        }
    }

    // 0.11x 的 ModelIdSerializationCache 用 _allEpochs 建 net id，只改 _allEpochIds 会在联机包/回放里抛异常。
    private static void RegisterEpochsInAllEpochs()
    {
        FieldInfo? field = typeof(EpochModel).GetField("_allEpochs", BindingFlags.Static | BindingFlags.NonPublic);
        if (field?.GetValue(null) is not List<Type> allEpochs)
        {
            Log.Warn("[TouhouAncients] EpochModel._allEpochs 不可用，节点无法进入联网 id 表。");
            return;
        }

        foreach (Registration registration in Epochs)
        {
            if (!allEpochs.Contains(registration.EpochType))
            {
                allEpochs.Add(registration.EpochType);
            }
        }
    }

    private static void AssignLayout()
    {
        foreach (Registration registration in Epochs)
        {
            EpochEra era = registration.Era.Value;
            int position = AllocatePosition(era, registration.Position);
            EraByType[registration.EpochType] = registration.Era;
            PositionByType[registration.EpochType] = position;
            ColumnNameKeys.TryAdd(era, TouhouAncientTimelineEra.DoorNameKey);
        }
    }

    private static int AllocatePosition(EpochEra era, int? requested)
    {
        if (requested.HasValue)
        {
            if (!Occupied.Add((era, requested.Value)))
            {
                throw new InvalidOperationException($"时间线格位冲突：era={(int)era} position={requested.Value} 已被占用。");
            }

            return requested.Value;
        }

        for (int position = 0; position < MaxAutoPositionScan; position++)
        {
            if (Occupied.Add((era, position)))
            {
                return position;
            }
        }

        throw new InvalidOperationException($"era={(int)era} 在 0..{MaxAutoPositionScan - 1} 内没有空位。");
    }

    private static void RefreshAllEpochIds()
    {
        List<string> ids = EpochModel.AllEpochIds.ToList();
        HashSet<string> known = ids.ToHashSet(StringComparer.Ordinal);

        // 只追加，保持原版顺序，避免多人 net id 偏移。
        foreach (string id in RegisteredIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            if (known.Add(id))
            {
                ids.Add(id);
            }
        }

        FieldInfo? field = typeof(EpochModel).GetField("_allEpochIds", BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, ids);
        typeof(EpochModel)
            .GetField("_epochIdsHashSet", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, null);
    }

    private static T GetStaticField<T>(Type type, string name)
    {
        FieldInfo field = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(type.FullName, name);
        return (T)field.GetValue(null)!;
    }

    private sealed record Registration(
        Type EpochType,
        TouhouAncientTimelineEra Era,
        TouhouTimelineUnlock Unlock,
        int? Position);
}
