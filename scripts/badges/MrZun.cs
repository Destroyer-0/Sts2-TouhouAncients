using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.potions;

namespace TouhouAncients.Scripts.badges;

/// <summary>一局内累计喝下 12 杯超ZUN啤酒。</summary>
public class MrZun : TouhouAncientBadge
{
    public const int RequiredDrinks = 12;

    public MrZun() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Gold;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) =>
        Drinks(run, player) >= RequiredDrinks;

    /// <summary>核心存档按地图节点记了本玩家用过的药水（PotionModel.OnUseWrapper 写入），无需自己追踪。</summary>
    private int Drinks(SerializableRun run, SerializablePlayer player)
    {
        ModelId beerId = ModelDb.Potion<SuperZunBeerPotion>().Id;
        return AllPoints(run)
            .SelectMany(point => point.PlayerStats.Where(stat => stat.PlayerId == player.NetId))
            .Sum(stat => stat.PotionUsed.Count(id => id == beerId));
    }
}
