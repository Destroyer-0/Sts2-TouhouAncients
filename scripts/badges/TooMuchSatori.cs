using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.badges;

/// <summary>一局内出现「satori」文本的房间数：2/3/4 对应铜/银/金。</summary>
public class TooMuchSatori : TouhouAncientBadge
{
    public TooMuchSatori() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => Count(run) switch
    {
        >= 4 => BadgeRarity.Gold,
        3 => BadgeRarity.Silver,
        2 => BadgeRarity.Bronze,
        _ => BadgeRarity.None,
    };

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) => Count(run) >= 2;

    /// <summary>每个房间最多计 1 次：房间 model id 或任一怪物 id 含 satori 即算。</summary>
    private static int Count(SerializableRun run) =>
        AllRooms(run).Count(room =>
            HasText(room.ModelId?.Entry, "satori") ||
            room.MonsterIds.Any(monsterId => HasText(monsterId.Entry, "satori")));
}
