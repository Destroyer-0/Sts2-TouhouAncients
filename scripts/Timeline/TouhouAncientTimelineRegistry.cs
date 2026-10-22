using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;

namespace TouhouAncients.Scripts.Timeline;

/// <summary>历史节点的注册、布局与槽位入口；在 Entry.Init 内调用一次 Initialize。</summary>
public static partial class TouhouAncientTimelineRegistry
{
    private const int MaxAutoPositionScan = 128;

    private static readonly object Sync = new();
    private static readonly List<StoryRegistration> Stories = [];
    private static readonly List<EpochRegistration> Epochs = [];
    private static readonly List<(Type From, Type To)> Expansions = [];
    private static readonly Dictionary<Type, TouhouAncientTimelineEra> EraByEpoch = [];
    private static readonly Dictionary<Type, int> PositionByEpoch = [];
    private static readonly Dictionary<Type, string> StoryIdByEpoch = [];
    private static readonly Dictionary<Type, List<Type>> StoryChapters = [];
    private static readonly Dictionary<Type, List<Type>> ExpansionByEpoch = [];
    private static readonly Dictionary<EpochEra, TouhouAncientTimelineEra> EraDescriptors = [];
    private static readonly HashSet<string> RegisteredIds = new(StringComparer.Ordinal);
    private static readonly HashSet<(EpochEra Era, int Position)> Occupied = [];

    private static bool _initialized;

    public static bool IsInitialized => _initialized;

    /// <summary>本 Mod 已注册的全部时间线节点 Id。</summary>
    public static IReadOnlySet<string> RegisteredEpochIds => RegisteredIds;

    /// <summary>解析特性并写入游戏字典；重复调用无副作用。</summary>
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
                RunStaticConstructors();
                ScanAttributes();
                ApplyRegistrations();
                _initialized = true;
                Log.Info($"[TouhouAncients] Timeline registered: {Stories.Count} stories, {Epochs.Count} epochs.");
            }
            catch (Exception ex)
            {
                Log.Error($"[TouhouAncients] Timeline registration failed: {ex}");
            }
        }
    }

    public static void RegisterStory<TStory>(string storyKey) where TStory : StoryModel, new()
    {
        EnsureMutable();
        AddStory(typeof(TStory), storyKey);
    }

    public static void RegisterEpoch<TEpoch>(TouhouAncientTimelineEra era, int? position = null, bool isRoot = false)
        where TEpoch : TouhouAncientTimeline, new()
    {
        EnsureMutable();
        AddEpoch(typeof(TEpoch), era, position, isRoot: isRoot);
    }

    /// <summary>把已注册节点按顺序绑定到故事；须先注册节点。</summary>
    public static void RegisterStoryChapter<TStory, TEpoch>(int order = 0)
        where TStory : TouhouAncientTimelineStory, new()
        where TEpoch : TouhouAncientTimeline, new()
    {
        EnsureMutable();
        bool declaredByAttribute = typeof(TStory).GetCustomAttribute<TouhouTimelineStoryAttribute>() != null;
        if (!declaredByAttribute && !Stories.Any(s => s.StoryType == typeof(TStory)))
        {
            throw new InvalidOperationException($"故事 {typeof(TStory).Name} 尚未注册，先调 RegisterStory。");
        }

        int index = Epochs.FindIndex(e => e.EpochType == typeof(TEpoch));
        if (index < 0)
        {
            throw new InvalidOperationException($"节点 {typeof(TEpoch).Name} 尚未注册，先调 RegisterEpoch。");
        }

        Epochs[index] = Epochs[index] with { StoryType = typeof(TStory), ChapterOrder = order };
    }

    public static void AddExpansion<TFrom, TTo>()
        where TFrom : TouhouAncientTimeline
        where TTo : TouhouAncientTimeline
    {
        EnsureMutable();
        AddExpansion(typeof(TFrom), typeof(TTo));
    }

    public static EpochEra ResolveEra(Type epochType)
        => EraByEpoch.TryGetValue(epochType, out TouhouAncientTimelineEra? era)
            ? era.Value
            : throw new InvalidOperationException($"时间线节点 {epochType.Name} 未注册。");

    public static int ResolvePosition(Type epochType)
        => PositionByEpoch.TryGetValue(epochType, out int position)
            ? position
            : throw new InvalidOperationException($"时间线节点 {epochType.Name} 未注册。");

    public static string? ResolveStoryId(Type epochType)
        => StoryIdByEpoch.TryGetValue(epochType, out string? storyId) ? storyId : null;

    public static EpochModel[] ResolveExpansion(Type epochType)
    {
        if (!ExpansionByEpoch.TryGetValue(epochType, out List<Type>? types))
        {
            return [];
        }

        return types.Select(type => EpochModel.Get(EpochModel.GetId(type))).ToArray();
    }

    public static EpochModel[] ResolveStoryEpochs(Type storyType)
    {
        if (!StoryChapters.TryGetValue(storyType, out List<Type>? types))
        {
            return [];
        }

        return types.Select(type => EpochModel.Get(EpochModel.GetId(type))).ToArray();
    }

    public static bool TryGetEra(EpochEra era, out TouhouAncientTimelineEra? descriptor)
        => EraDescriptors.TryGetValue(era, out descriptor);

    /// <summary>补开故事根节点与无故事节点的槽位；幂等。</summary>
    public static void EnsureRootSlots() => EnsureRootSlots(SaveManager.Instance.Progress);

    public static void EnsureRootSlots(ProgressState? progress)
    {
        if (!_initialized || progress == null)
        {
            return;
        }

        foreach (Type type in GetRootEpochTypes())
        {
            string id;
            try
            {
                id = EpochModel.GetId(type);
            }
            catch (Exception ex)
            {
                Log.Error($"[TouhouAncients] 无法解析时间线节点 {type.Name} 的 Id：{ex.Message}");
                continue;
            }

            if (!RegisteredIds.Contains(id))
            {
                continue;
            }

            SerializableEpoch? entry = progress.Epochs.FirstOrDefault(e => e.Id == id);
            if (entry == null)
            {
                progress.UnlockSlot(id);
            }
            else if (entry.State == EpochState.ObtainedNoSlot)
            {
                progress.UnlockSlot(id);
            }
            else if (entry.State == EpochState.None)
            {
                progress.ObtainEpochOverride(id, EpochState.NotObtained);
            }
        }
    }

    /// <summary>清空本 Mod 全部节点进度并恢复成首次安装的根槽位；返回清掉的条目数。</summary>
    public static int ClearAllProgress(ProgressState? progress = null)
    {
        progress ??= SaveManager.Instance.Progress;
        if (!_initialized || progress == null)
        {
            return 0;
        }

        int removed;
        if (progress.Epochs is List<SerializableEpoch> epochs)
        {
            // 直接删条目 = 首次安装状态；留着条目而是 NotObtained 会让章节节点提前出现
            removed = epochs.RemoveAll(entry => RegisteredIds.Contains(entry.Id));
        }
        else
        {
            removed = 0;
            foreach (string id in RegisteredIds)
            {
                progress.ObtainEpochOverride(id, EpochState.NotObtained);
            }

            Log.Warn("[TouhouAncients] ProgressState.Epochs 不是 List，改用状态回退清除。");
        }

        EnsureRootSlots(progress);
        return removed;
    }

    private static void RunStaticConstructors()
    {
        // 先让原版静态构造把字典填满，再追加 Mod 节点。
        RuntimeHelpers.RunClassConstructor(typeof(EpochModel).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(StoryModel).TypeHandle);
    }

    private static void ScanAttributes()
    {
        Type[] types = GetLoadableTypes(typeof(TouhouAncientTimelineRegistry).Assembly);

        foreach (Type type in types)
        {
            if (type.IsAbstract || type.IsInterface || !typeof(StoryModel).IsAssignableFrom(type))
            {
                continue;
            }

            TouhouTimelineStoryAttribute? attribute = type.GetCustomAttribute<TouhouTimelineStoryAttribute>();
            if (attribute != null)
            {
                AddStory(type, attribute.StoryKey);
            }
        }

        foreach (Type type in types)
        {
            if (type.IsAbstract || type.IsInterface || !typeof(TouhouAncientTimeline).IsAssignableFrom(type))
            {
                continue;
            }

            TouhouTimelineEpochAttribute? attribute = type.GetCustomAttribute<TouhouTimelineEpochAttribute>();
            if (attribute == null)
            {
                continue;
            }

            TouhouAncientTimelineEra era = Enum.IsDefined(typeof(EpochEra), attribute.EraValue)
                ? TouhouAncientTimelineEra.AppendTo((EpochEra)attribute.EraValue)
                : TouhouAncientTimelineEra.Custom(
                    attribute.EraValue,
                    attribute.EraSlug ?? $"touhou_era_{attribute.EraValue}",
                    attribute.EraIconPath);

            StoryRegistration? story = FindStory(attribute.StoryKey);
            if (!string.IsNullOrWhiteSpace(attribute.StoryKey) && story == null)
            {
                Log.Warn($"[TouhouAncients] 节点 {type.Name} 的 StoryKey={attribute.StoryKey} 未注册，将按独立节点处理。");
            }

            AddEpoch(
                type,
                era,
                attribute.Position >= 0 ? attribute.Position : null,
                story?.StoryType,
                attribute.ChapterOrder,
                attribute.Root);
        }
    }

    private static void ApplyRegistrations()
    {
        SeedVanillaOccupancy();
        RegisterStoriesInGame();
        RegisterEpochsInGame();
        RegisterEpochsInAllEpochs();
        AssignLayout();
        BuildStoryData();
        RefreshAllEpochIds();
    }

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

    private static void RegisterStoriesInGame()
    {
        Dictionary<string, Type> storyTypes = GetStaticField<Dictionary<string, Type>>(typeof(StoryModel), "_storyTypeDictionary");

        foreach (StoryRegistration registration in Stories)
        {
            StoryModel story = (StoryModel)Activator.CreateInstance(registration.StoryType)!;
            string actualId = GetStoryModelId(story);
            if (!string.Equals(actualId, registration.StoryId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"故事 {registration.StoryType.Name} 的 StoryKey 与注册键不一致：模型 Id={actualId}，注册 Id={registration.StoryId}。");
            }

            if (storyTypes.TryGetValue(registration.StoryId, out Type? existing) && existing != registration.StoryType)
            {
                throw new InvalidOperationException(
                    $"故事 Id 冲突：{registration.StoryId} 已被 {existing.Name} 占用（{registration.StoryType.Name}）。");
            }

            storyTypes[registration.StoryId] = registration.StoryType;
        }
    }

    private static void RegisterEpochsInGame()
    {
        Dictionary<string, Type> epochTypes = GetStaticField<Dictionary<string, Type>>(typeof(EpochModel), "_epochTypeDictionary");
        Dictionary<Type, string> typeToId = GetStaticField<Dictionary<Type, string>>(typeof(EpochModel), "_typeToIdDictionary");

        foreach (EpochRegistration registration in Epochs)
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

    // 0.11x 的 ModelIdSerializationCache 用 AllEpochs（编译期固定的本体类型表）建 net id，
    // 只改 _allEpochIds 不进表 → 写 replay / 联机包时 GetNetIdForEpochId 抛异常。
    private static void RegisterEpochsInAllEpochs()
    {
        FieldInfo? field = typeof(EpochModel).GetField("_allEpochs", BindingFlags.Static | BindingFlags.NonPublic);
        if (field?.GetValue(null) is not List<Type> allEpochs)
        {
            Log.Warn("[TouhouAncients] EpochModel._allEpochs 不可用，节点无法进入联网 id 表。");
            return;
        }

        foreach (EpochRegistration registration in Epochs)
        {
            if (!allEpochs.Contains(registration.EpochType))
            {
                allEpochs.Add(registration.EpochType);
            }
        }
    }

    private static void AssignLayout()
    {
        foreach (EpochRegistration registration in Epochs)
        {
            TouhouAncientTimelineEra era = registration.Era
                ?? throw new InvalidOperationException($"节点 {registration.EpochType.Name} 缺少时代列。");

            int position = AllocatePosition(era.Value, registration.Position);
            EraByEpoch[registration.EpochType] = era;
            PositionByEpoch[registration.EpochType] = position;

            if (EraDescriptors.TryGetValue(era.Value, out TouhouAncientTimelineEra? existing))
            {
                if (!string.Equals(existing.Slug, era.Slug, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"era={(int)era.Value} 已绑定 slug={existing.Slug}，不能再绑定 {era.Slug}。");
                }
            }
            else
            {
                EraDescriptors[era.Value] = era;
            }
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

    private static void BuildStoryData()
    {
        foreach (IGrouping<Type, EpochRegistration> group in Epochs
                     .Where(e => e.StoryType != null)
                     .GroupBy(e => e.StoryType!))
        {
            List<EpochRegistration> ordered = group.OrderBy(e => e.ChapterOrder).ToList();
            StoryChapters[group.Key] = ordered.Select(e => e.EpochType).ToList();

            string storyId = GetStoryKey(group.Key);
            foreach (EpochRegistration registration in ordered)
            {
                StoryIdByEpoch[registration.EpochType] = storyId;
            }

            // Root 节点是并列起点，不与上一章连边；展开链只在段内连
            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].IsRoot)
                {
                    continue;
                }

                AddExpansion(ordered[i - 1].EpochType, ordered[i].EpochType);
            }
        }

        foreach ((Type from, Type to) in Expansions)
        {
            if (!Epochs.Any(e => e.EpochType == from) || !Epochs.Any(e => e.EpochType == to))
            {
                throw new InvalidOperationException($"展开链引用了未注册节点：{from.Name} -> {to.Name}。");
            }

            if (!ExpansionByEpoch.TryGetValue(from, out List<Type>? list))
            {
                list = [];
                ExpansionByEpoch[from] = list;
            }

            if (!list.Contains(to))
            {
                list.Add(to);
            }
        }
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

    private static IEnumerable<Type> GetRootEpochTypes()
    {
        HashSet<Type> roots = [];
        foreach (EpochRegistration registration in Epochs)
        {
            // 故事首章、Root 并列节点、无故事节点都要开槽
            bool isRoot = registration.StoryType == null
                          || registration.IsRoot
                          || (StoryChapters.TryGetValue(registration.StoryType, out List<Type>? chapters)
                              && chapters.Count > 0
                              && chapters[0] == registration.EpochType);

            if (isRoot)
            {
                roots.Add(registration.EpochType);
            }
        }

        return roots;
    }

    private static void AddStory(Type storyType, string storyKey)
    {
        if (string.IsNullOrWhiteSpace(storyKey))
        {
            throw new ArgumentException($"故事 {storyType.Name} 的 StoryKey 不能为空。", nameof(storyKey));
        }

        if (storyType.IsAbstract || storyType.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new ArgumentException($"故事 {storyType.Name} 必须是带无参构造的具体类型。", nameof(storyType));
        }

        StoryRegistration? existing = Stories.FirstOrDefault(s => s.StoryType == storyType);
        if (existing != null)
        {
            string storyId = StringHelper.Slugify(storyKey);
            if (!string.Equals(existing.StoryId, storyId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"故事 {storyType.Name} 已用 {existing.StoryKey} 注册，不能再注册为 {storyKey}。");
            }

            Log.Warn($"[TouhouAncients] 故事重复注册已跳过：{storyType.Name}");
            return;
        }

        Stories.Add(new StoryRegistration(storyType, storyKey.Trim(), StringHelper.Slugify(storyKey)));
    }

    private static void AddEpoch(
        Type epochType,
        TouhouAncientTimelineEra era,
        int? position,
        Type? storyType = null,
        int chapterOrder = 0,
        bool isRoot = false)
    {
        if (epochType.IsAbstract || epochType.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new ArgumentException($"节点 {epochType.Name} 必须是带无参构造的具体类型。", nameof(epochType));
        }

        if (Epochs.Any(e => e.EpochType == epochType))
        {
            Log.Warn($"[TouhouAncients] 节点重复注册已跳过：{epochType.Name}");
            return;
        }

        Epochs.Add(new EpochRegistration(epochType, era, position, storyType, chapterOrder, isRoot));
    }

    private static void AddExpansion(Type from, Type to)
    {
        if (!Expansions.Contains((from, to)))
        {
            Expansions.Add((from, to));
        }
    }

    private static StoryRegistration? FindStory(string? storyKey)
    {
        if (string.IsNullOrWhiteSpace(storyKey))
        {
            return null;
        }

        string id = StringHelper.Slugify(storyKey);
        return Stories.FirstOrDefault(s => string.Equals(s.StoryId, id, StringComparison.Ordinal));
    }

    private static string GetStoryModelId(StoryModel story)
    {
        PropertyInfo? property = typeof(StoryModel).GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic);
        return property?.GetValue(story) as string ?? string.Empty;
    }

    private static string GetStoryKey(Type storyType)
        => Stories.FirstOrDefault(s => s.StoryType == storyType)?.StoryKey
           ?? throw new InvalidOperationException($"故事 {storyType.Name} 未注册。");

    private static void EnsureMutable()
    {
        if (_initialized)
        {
            throw new InvalidOperationException("时间线注册已冻结；请在 Entry.Init 的 Initialize 之前注册。");
        }
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null).Select(type => type!).ToArray();
        }
    }

    private static T GetStaticField<T>(Type type, string name)
    {
        FieldInfo field = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(type.FullName, name);
        return (T)field.GetValue(null)!;
    }

    private sealed record StoryRegistration(Type StoryType, string StoryKey, string StoryId);

    private sealed record EpochRegistration(
        Type EpochType,
        TouhouAncientTimelineEra Era,
        int? Position,
        Type? StoryType = null,
        int ChapterOrder = 0,
        bool IsRoot = false);
}
