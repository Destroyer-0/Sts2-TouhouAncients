using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts;

public static class TouhouAncientCmd
{
    public static string? CheckPathExists(string path) => ResourceLoader.Exists(path) ? path : null;

    public static string CheckPathExistsWithFallback(string path, string alternative) =>
        ResourceLoader.Exists(path) ? path : alternative;
    public static string? CheckPathExistsWithFallback2(string path, string? alternative) =>
        ResourceLoader.Exists(path) ? path : alternative;
    
    public static bool IsScry(CardModel card)
    {
        return card.Keywords.Contains(TouhouAncientKeywords.TouhouAncientSatoriScry);
    }
    public static bool IsKoishi(CardModel card)
    {
        return card.Keywords.Contains(TouhouAncientKeywords.TouhouAncientKoishiUnplayable);
    }

    public static bool IsPlayerDamageIncludePet(Player player, Creature? dealer)
    {
        if (dealer == player.Creature) return true;
        return dealer is { IsPet: true } && dealer.PetOwner?.Creature == player.Creature;
    }

    public static bool IsPlayerDamageIncludePet(Player player, ref Creature? dealer)
    {
        if (dealer == player.Creature) return true;
        if (dealer is { IsPet: true } && dealer.PetOwner?.Creature == player.Creature)
        {
            dealer = dealer.PetOwner.Creature;
            return true;
        }
        return false;
    }
    public static bool IsPlayerDamageIncludePet(Creature? player, Creature? dealer)
    {
        if (dealer == null) return false;
        if (dealer == player) return true;
        return dealer is { IsPet: true } && dealer.PetOwner?.Creature == player;
    }

    /// <summary>本 Mod 全部先古之民（含未解锁），按 Id 排序保证各端一致。</summary>
    internal static List<TouhouAncientBase> AllAncients()
    {
        var result = new List<TouhouAncientBase>();
        foreach (Type type in ModelDb.AllAbstractModelSubtypes)
        {
            if (type == null || type.IsAbstract || type.IsInterface || !typeof(TouhouAncientBase).IsAssignableFrom(type))
            {
                continue;
            }

            if (ModelDb.GetByIdOrNull<AncientEventModel>(ModelDb.GetId(type)) is TouhouAncientBase ancient)
            {
                result.Add(ancient);
            }
        }

        result.Sort((left, right) => string.Compare(left.Id.Entry, right.Id.Entry, StringComparison.Ordinal));
        return result;
    }

    /// <summary>中文别名 → 先古之民 Id。只放"标题里搜不到"的写法，其余靠标题子串匹配。</summary>
    private static readonly Dictionary<string, string> AncientAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["依神女苑"] = "TOUHOUANCIENTS-YORIGAMI_SISTER_ANCIENT",
        ["依神紫苑"] = "TOUHOUANCIENTS-YORIGAMI_SISTER_ANCIENT",
        ["依神姐妹"] = "TOUHOUANCIENTS-YORIGAMI_SISTER_ANCIENT",
        ["女苑"] = "TOUHOUANCIENTS-YORIGAMI_SISTER_ANCIENT",
        ["紫苑"] = "TOUHOUANCIENTS-YORIGAMI_SISTER_ANCIENT",
        ["文文"] = "TOUHOUANCIENTS-SHAMYOUMARU_AYA_ANCIENT",
    };

    /// <summary>
    /// 按 Id 或中文名找先古之民：Id 精确 → 别名表 → 标题精确 → 标题子串（"灵梦"命中"博丽 灵梦"）。
    /// 子串命中多个时取标题最短的（最贴近简称），仍并列则取 Id 序第一个。
    /// </summary>
    internal static TouhouAncientBase? FindAncient(string query)
    {
        var ancients = AllAncients();
        string key = query.Trim();
        if (key.Length == 0)
        {
            return null;
        }

        string upper = key.ToUpperInvariant();
        var byId = ancients.FirstOrDefault(ancient => ancient.Id.Entry == upper);
        if (byId != null)
        {
            return byId;
        }

        if (AncientAliases.TryGetValue(key, out string? aliasId))
        {
            var aliased = ancients.FirstOrDefault(ancient => ancient.Id.Entry == aliasId);
            if (aliased != null)
            {
                return aliased;
            }
        }

        var titled = ancients
            .Select(ancient => (ancient, title: AncientTitle(ancient)))
            .Where(pair => pair.title.Length > 0)
            .ToList();

        var exact = titled.FirstOrDefault(pair => pair.title == key);
        if (exact.ancient != null)
        {
            return exact.ancient;
        }

        return titled
            .Where(pair => pair.title.Contains(key, StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.title.Length)
            .ThenBy(pair => pair.ancient.Id.Entry, StringComparer.Ordinal)
            .Select(pair => pair.ancient)
            .FirstOrDefault();
    }

    /// <summary>先古之民标题（ancients 表 {Id}.title），取不到时返回空串。</summary>
    internal static string AncientTitle(AncientEventModel ancient)
    {
        string key = ancient.Id.Entry + ".title";
        return LocString.Exists("ancients", key) ? new LocString("ancients", key).GetRawText() : "";
    }

    /// <summary>按 Id 或中文名找遗物：Id 精确 → 标题精确 → 标题子串（取最短标题）。</summary>
    internal static RelicModel? FindRelic(string query)
    {
        string key = query.Trim();
        if (key.Length == 0)
        {
            return null;
        }

        string upper = key.ToUpperInvariant();
        var relics = ModelDb.AllRelics.ToList();
        var byId = relics.FirstOrDefault(relic => relic.Id.Entry == upper);
        if (byId != null)
        {
            return byId;
        }

        var titled = relics
            .Select(relic => (relic, title: RelicTitle(relic)))
            .Where(pair => pair.title.Length > 0)
            .ToList();

        var exact = titled.FirstOrDefault(pair => pair.title == key);
        if (exact.relic != null)
        {
            return exact.relic;
        }

        return titled
            .Where(pair => pair.title.Contains(key, StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.title.Length)
            .ThenBy(pair => pair.relic.Id.Entry, StringComparer.Ordinal)
            .Select(pair => pair.relic)
            .FirstOrDefault();
    }

    /// <summary>遗物标题（relics 表 {Id}.title），取不到时返回空串。</summary>
    internal static string RelicTitle(RelicModel relic)
    {
        string key = relic.Id.Entry + ".title";
        return LocString.Exists("relics", key) ? new LocString("relics", key).GetRawText() : "";
    }
}

/// <summary>thafight {先古之民}：直接进入该先古之民的挑战遭遇。</summary>
public sealed class ThaFightConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "thafight";

    public override string Args => "<ancient:string>";

    public override string Description => "Jumps to a TouhouAncients ancient's challenge encounter (id or Chinese name).";

    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0)
        {
            return new CmdResult(success: false, "No ancient specified.");
        }

        if (!RunManager.Instance.IsInProgress)
        {
            return new CmdResult(success: false, "A run is currently not in progress!");
        }

        var ancient = TouhouAncientCmd.FindAncient(args[0]);
        if (ancient == null)
        {
            return new CmdResult(success: false, $"Ancient '{args[0]}' not found");
        }

        if (ancient.ChallengeEncounter is not { } challenge)
        {
            return new CmdResult(success: false, $"Ancient '{ancient.Id.Entry}' has no challenge encounter");
        }

        var encounter = challenge.ToMutable();
        encounter.DebugRandomizeRng();
        Task task = RunManager.Instance.EnterRoomDebug(RoomType.Monster, MapPointType.Unassigned, encounter);
        return new CmdResult(task, success: true, $"Jumped to encounter: '{encounter.Id.Entry}'");
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
        {
            var candidates = TouhouAncientCmd.AllAncients()
                .Where(ancient => ancient.ChallengeEncounter != null)
                .Select(ancient => ancient.Id.Entry)
                .ToList();
            return CompleteArgument(candidates, Array.Empty<string>(), args.FirstOrDefault() ?? "");
        }

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName
        };
    }
}

/// <summary>thaancient {先古之民} [遗物]：打开先古之民事件，可选强制某个遗物出现在第一个合法位置。</summary>
public sealed class ThaAncientConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "thaancient";

    public override string Args => "<ancient:string> [relic:string]";

    public override string Description => "Opens a TouhouAncients ancient event; optional relic is forced into the first legal slot.";

    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0)
        {
            return new CmdResult(success: false, "No ancient specified.");
        }

        if (!RunManager.Instance.IsInProgress)
        {
            return new CmdResult(success: false, "A run is currently not in progress!");
        }

        var ancient = TouhouAncientCmd.FindAncient(args[0]);
        if (ancient == null)
        {
            return new CmdResult(success: false, $"Ancient '{args[0]}' not found");
        }

        ModelId? forcedRelicId = null;
        if (args.Length > 1)
        {
            var relic = TouhouAncientCmd.FindRelic(args[1]);
            if (relic == null)
            {
                return new CmdResult(success: false, $"Relic '{args[1]}' not found");
            }

            forcedRelicId = relic.Id;
        }

        var room = new EventRoom(ancient)
        {
            OnStart = forcedRelicId == null ? null : eventModel => ForceRelicOption(eventModel, forcedRelicId)
        };
        issuingPlayer!.RunState.AppendToMapPointHistory(MapPointType.Ancient, RoomType.Event, ancient.Id);
        Task task = RunManager.Instance.EnterRoom(room);
        return new CmdResult(task, success: true,
            forcedRelicId == null
                ? $"Opened Ancient Event: '{ancient.Id.Entry}'"
                : $"Opened Ancient Event: '{ancient.Id.Entry}', forcing relic '{forcedRelicId.Entry}'");
    }

    /// <summary>
    /// 把指定遗物挪到第一个合法位置。OnStart 早于 GenerateInitialOptionsWrapper，
    /// 所以这里改的是 mutable 实例上的字段，生成选项时直接生效。
    /// </summary>
    private static void ForceRelicOption(EventModel eventModel, ModelId relicId)
    {
        if (eventModel is not TouhouAncientBase ancient)
        {
            return;
        }

        ancient.ForcedRelicId = relicId;
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
        {
            var candidates = TouhouAncientCmd.AllAncients().Select(ancient => ancient.Id.Entry).ToList();
            return CompleteArgument(candidates, Array.Empty<string>(), args.FirstOrDefault() ?? "");
        }

        if (args.Length == 2)
        {
            var candidates = ModelDb.AllRelics.Select(relic => relic.Id.Entry).ToList();
            return CompleteArgument(candidates, [args[0]], args[1]);
        }

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName
        };
    }
}

/// <summary>tharelic {遗物}：按中文名（或 Id）获得遗物，等价于原版 relic 命令。</summary>
public sealed class ThaRelicConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "tharelic";

    public override string Args => "<relic:string>";

    public override string Description => "Adds a relic to the player, resolved by Chinese name or id.";

    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0)
        {
            return new CmdResult(success: false, "No relic specified.");
        }

        if (issuingPlayer == null)
        {
            return new CmdResult(success: false, "A run is currently not in progress!");
        }

        var relic = TouhouAncientCmd.FindRelic(args[0]);
        if (relic == null)
        {
            return new CmdResult(success: false, $"Relic '{args[0]}' not found");
        }

        TaskHelper.RunSafely(RelicCmd.Obtain(relic.ToMutable(), issuingPlayer));
        return new CmdResult(success: true, $"Added relic '{TouhouAncientCmd.RelicTitle(relic)}' ({relic.Id.Entry})");
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
        {
            var candidates = ModelDb.AllRelics.Select(relic => relic.Id.Entry).ToList();
            return CompleteArgument(candidates, Array.Empty<string>(), args.FirstOrDefault() ?? "");
        }

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName
        };
    }
}