using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.badges;

/// <summary>一局内遇到「觉」的次数：2/3/4 对应铜/银/金；角色本人也算 1 次。</summary>
public class TooMuchSatori : TouhouAncientBadge
{
    public TooMuchSatori() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => Count(run, player) switch
    {
        >= 4 => BadgeRarity.Gold,
        3 => BadgeRarity.Silver,
        2 => BadgeRarity.Bronze,
        _ => BadgeRarity.None,
    };

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) => Count(run, player) >= 2;

    /// <summary>每个房间最多计 1 次：房间 id 或任一怪物 id 的模型类型名含 satori 即算。</summary>
    private static int Count(SerializableRun run, SerializablePlayer player) =>
        AllRooms(run).Count(room =>
            ModelNameContains(room.ModelId, "satori") ||
            room.MonsterIds.Any(monsterId => ModelNameContains(monsterId, "satori"))) +
        (ModelNameContains(player.CharacterId, "satori") ? 1 : 0);
}
