using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;

namespace TouhouAncients.Scripts.Timeline;

/// <summary>规则引擎的求值上下文；局末与战斗结束各填一部分。</summary>
public sealed class TouhouTimelineUnlockContext
{
    public SerializableRun? Run { get; init; }

    public SerializablePlayer? RunPlayer { get; init; }

    public Player? CombatPlayer { get; init; }

    public CombatRoom? CombatRoom { get; init; }

    public AncientEventModel? Ancient { get; init; }

    public bool Victory { get; init; }

    public ProgressState Progress => SaveManager.Instance.Progress;
}

/// <summary>解锁条件；满足任一即获得对应节点。</summary>
public sealed class TouhouTimelineUnlockRule
{
    private readonly Func<TouhouTimelineUnlockContext, bool> _matches;

    private TouhouTimelineUnlockRule(Func<TouhouTimelineUnlockContext, bool> matches)
    {
        _matches = matches;
    }

    internal bool Matches(TouhouTimelineUnlockContext context) => _matches(context);

    public static TouhouTimelineUnlockRule AfterRunAs(string characterEntry)
        => new(ctx => HasCharacter(ctx, characterEntry));

    public static TouhouTimelineUnlockRule AfterWinAs(string characterEntry)
        => new(ctx => ctx.Victory && HasCharacter(ctx, characterEntry));

    public static TouhouTimelineUnlockRule AfterVictory()
        => new(ctx => ctx.Run != null && ctx.Victory);

    public static TouhouTimelineUnlockRule AfterAscensionWin(int level)
        => new(ctx => ctx.Run != null && ctx.Victory && ctx.Run.Ascension >= level);

    public static TouhouTimelineUnlockRule AfterRunCount(int count, bool requireVictory = false)
        => new(ctx => ctx.Run != null &&
                      (requireVictory ? ctx.Progress.Wins : ctx.Progress.NumberOfRuns) >= count);

    public static TouhouTimelineUnlockRule AfterDefeatEncounter(string encounterEntry)
        => new(ctx => ctx.CombatRoom != null &&
                      IsCleared(ctx.CombatRoom) &&
                      string.Equals(ctx.CombatRoom.Encounter.Id.Entry, encounterEntry, StringComparison.OrdinalIgnoreCase));

    public static TouhouTimelineUnlockRule AfterEliteVictories(int count)
        => new(ctx => ctx.CombatRoom?.RoomType == RoomType.Elite &&
                      IsCleared(ctx.CombatRoom) &&
                      CountWins(ctx.Progress, RoomType.Elite) >= count);

    public static TouhouTimelineUnlockRule AfterBossVictories(int count)
        => new(ctx => ctx.CombatRoom?.RoomType == RoomType.Boss &&
                      IsCleared(ctx.CombatRoom) &&
                      CountWins(ctx.Progress, RoomType.Boss) >= count);

    public static TouhouTimelineUnlockRule AfterClearAct(int actIndex)
        => new(ctx => ctx.CombatRoom?.RoomType == RoomType.Boss &&
                      IsCleared(ctx.CombatRoom) &&
                      ctx.CombatPlayer?.RunState.CurrentActIndex == actIndex);

    public static TouhouTimelineUnlockRule AfterEncounterAncient<TAncient>()
        where TAncient : AncientEventModel
        => new(ctx => ctx.Ancient is TAncient);

    public static TouhouTimelineUnlockRule AfterEncounterAncient(string ancientEntry)
        => new(ctx => ctx.Ancient != null &&
                      string.Equals(ctx.Ancient.Id.Entry, ancientEntry, StringComparison.OrdinalIgnoreCase));

    public static TouhouTimelineUnlockRule Custom(Func<TouhouTimelineUnlockContext, bool> predicate)
        => new(predicate ?? throw new ArgumentNullException(nameof(predicate)));

    private static bool HasCharacter(TouhouTimelineUnlockContext context, string characterEntry)
    {
        if (context.Run == null)
        {
            return false;
        }

        string? entry = context.RunPlayer?.CharacterId?.Entry;
        return string.Equals(entry, characterEntry, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCleared(CombatRoom room) => room.Enemies.All(enemy => enemy.IsDead);

    private static int CountWins(ProgressState progress, RoomType roomType)
    {
        int total = 0;
        foreach (KeyValuePair<ModelId, EncounterStats> pair in progress.EncounterStats)
        {
            EncounterModel? encounter = ModelDb.GetByIdOrNull<EncounterModel>(pair.Key);
            if (encounter?.RoomType == roomType)
            {
                total += pair.Value.TotalWins;
            }
        }

        return total;
    }
}

public static partial class TouhouAncientTimelineRegistry
{
    private static readonly Dictionary<Type, List<TouhouTimelineUnlockRule>> Rules = [];

    public static void AddRule<TEpoch>(params TouhouTimelineUnlockRule[] rules)
        where TEpoch : TouhouAncientTimeline
    {
        if (rules.Length == 0)
        {
            return;
        }

        if (!Rules.TryGetValue(typeof(TEpoch), out List<TouhouTimelineUnlockRule>? list))
        {
            list = [];
            Rules[typeof(TEpoch)] = list;
        }

        list.AddRange(rules);
    }

    public static bool TryObtain<TEpoch>(ICollection<string>? discovered = null)
        where TEpoch : TouhouAncientTimeline
        => TryObtain(typeof(TEpoch), discovered);

    public static bool TryObtain(Type epochType, ICollection<string>? discovered = null)
    {
        string id = EpochModel.GetId(epochType);
        SerializableEpoch? entry = SaveManager.Instance.Progress.Epochs.FirstOrDefault(e => e.Id == id);

        if (entry != null && entry.State >= EpochState.Obtained)
        {
            return false;
        }

        SaveManager.Instance.ObtainEpoch(id);
        if (discovered != null && !discovered.Contains(id))
        {
            discovered.Add(id);
        }

        return true;
    }

    public static void OpenSlot<TEpoch>() where TEpoch : TouhouAncientTimeline
    {
        string id = EpochModel.GetId(typeof(TEpoch));
        SerializableEpoch? entry = SaveManager.Instance.Progress.Epochs.FirstOrDefault(e => e.Id == id);
        if (entry == null || entry.State == EpochState.None)
        {
            SaveManager.Instance.ObtainEpochOverride(id, EpochState.NotObtained);
        }
    }

    public static void Reveal<TEpoch>() where TEpoch : TouhouAncientTimeline
    {
        string id = EpochModel.GetId(typeof(TEpoch));
        OpenSlot<TEpoch>();
        TryObtain<TEpoch>();

        SerializableEpoch? entry = SaveManager.Instance.Progress.Epochs.FirstOrDefault(e => e.Id == id);
        if (entry != null && entry.State >= EpochState.Obtained)
        {
            SaveManager.Instance.RevealEpoch(id, isDebug: true);
        }
    }

    public static bool IsRevealed<TEpoch>() where TEpoch : TouhouAncientTimeline
        => SaveManager.Instance.IsEpochRevealed(EpochModel.GetId(typeof(TEpoch)));

    internal static void EvaluateRunEnd(SerializableRun run, bool victory)
    {
        if (!_initialized || run.GameMode.AreAchievementsAndEpochsLocked())
        {
            return;
        }

        SerializablePlayer? local;
        try
        {
            local = LocalContext.GetMe(run);
        }
        catch (Exception)
        {
            local = null;
        }

        local ??= run.Players?.FirstOrDefault();
        TouhouTimelineUnlockContext context = new()
        {
            Run = run,
            RunPlayer = local,
            Victory = victory
        };

        EvaluateRules(context, local?.DiscoveredEpochs);
    }

    internal static void EvaluateCombatEnd(Player player, CombatRoom room)
    {
        if (!_initialized || player == null || room == null || player.RunState.GameMode.AreAchievementsAndEpochsLocked())
        {
            return;
        }

        TouhouTimelineUnlockContext context = new()
        {
            CombatPlayer = player,
            CombatRoom = room,
            Victory = true
        };

        EvaluateRules(context, player.DiscoveredEpochs);
    }

    internal static void EvaluateAncientEncounter(EventModel eventModel, Player owner)
    {
        if (!_initialized || eventModel is not AncientEventModel ancient || owner == null)
        {
            return;
        }

        // 多人里 EventSynchronizer 会为每个玩家各建一份事件实例，只处理本机账号。
        if (LocalContext.NetId.HasValue && !LocalContext.IsMe(owner))
        {
            return;
        }

        if (owner.RunState.GameMode.AreAchievementsAndEpochsLocked())
        {
            return;
        }

        TouhouTimelineUnlockContext context = new()
        {
            Ancient = ancient,
            CombatPlayer = owner,
            Victory = false
        };

        EvaluateRules(context, owner.DiscoveredEpochs);
    }

    private static void EvaluateRules(TouhouTimelineUnlockContext context, ICollection<string>? discovered)
    {
        foreach ((Type epochType, List<TouhouTimelineUnlockRule> rules) in Rules.ToArray())
        {
            try
            {
                if (rules.Any(rule => rule.Matches(context)))
                {
                    TryObtain(epochType, discovered);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[TouhouAncients] Timeline unlock rule failed for {epochType.Name}: {ex}");
            }
        }
    }
}
