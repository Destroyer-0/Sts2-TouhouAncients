using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.encounters;

namespace TouhouAncients.Scripts.badges;

/// <summary>
/// 东方徽章基类。BaseLib 自动扫描 CustomBadge 子类并注入 BadgePool，无需注册。
/// 图标主路径为 res://images/ui/game_over_screen/{类名}.png，缺失时回退到遗物图标。
/// </summary>
public abstract class TouhouAncientBadge(bool requiresWin, bool multiplayerOnly)
    : CustomBadge(requiresWin, multiplayerOnly)
{
    /// <summary>回退图标文件名（无扩展名），对应 images/icon/relics/{该值}.png。</summary>
    public virtual string DefaultFileName => "default";

    public override string? CustomBadgeIconPath => TouhouAncientCmd.CheckPathExistsWithFallback(
        $"res://images/ui/game_over_screen/{GetType().Name}.png",
        $"res://images/icon/relics/{DefaultFileName}.png");

    /// <summary>玩家是否持有指定遗物。</summary>
    protected static bool HasRelic<T>(SerializablePlayer player) where T : RelicModel =>
        player.Relics.Any(relic => relic.Id == ModelDb.Relic<T>().Id);

    /// <summary>玩家牌组中是否有指定牌。</summary>
    protected static bool HasCard<T>(SerializablePlayer player) where T : CardModel =>
        player.Deck.Any(card => card.Id == ModelDb.Card<T>().Id);

    /// <summary>忽略大小写的子串匹配。</summary>
    protected static bool HasText(string? text, string value) =>
        text != null && text.Contains(value, StringComparison.OrdinalIgnoreCase);

    /// <summary>本局进入过的全部房间。</summary>
    protected static IEnumerable<MapPointRoomHistoryEntry> AllRooms(SerializableRun run) =>
        AllPoints(run).SelectMany(point => point.Rooms);

    /// <summary>本局访问过的全部地图节点（MapPointHistory 按层分组）。</summary>
    protected static IEnumerable<MapPointHistoryEntry> AllPoints(SerializableRun run) =>
        run.MapPointHistory.SelectMany(act => act);

    /// <summary>该房间是否为东方先古之民的挑战战斗。</summary>
    protected static bool IsAncientChallengeRoom(MapPointRoomHistoryEntry? room) =>
        room?.ModelId is { } id && ModelDb.GetByIdOrNull<AbstractModel>(id) is TouhouAncientEncounter;
}
