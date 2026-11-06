using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.badges;

/// <summary>一局内遇到觉 4 次且恋 2 次；角色本人也算 1 次。</summary>
public class SatoriKoishi : TouhouAncientBadge
{
    private const int SatoriRequired = 4;
    private const int KoishiRequired = 2;

    public SatoriKoishi() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Gold;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) =>
        Count(run, player, "satori") >= SatoriRequired &&
        Count(run, player, "koishi") >= KoishiRequired;

    /// <summary>房间命中数 + 本机玩家角色命中 1 次。</summary>
    private static int Count(SerializableRun run, SerializablePlayer player, string value) =>
        AllRooms(run).Count(room =>
            ModelNameContains(room.ModelId, value) ||
            room.MonsterIds.Any(monsterId => ModelNameContains(monsterId, value))) +
        (ModelNameContains(player.CharacterId, value) ? 1 : 0);
}
