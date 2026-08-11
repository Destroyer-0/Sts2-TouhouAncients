using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.potions;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 鲵吞亭特调的 Harmony 补丁：在获取药水时，若玩家持有鲵吞亭特调，
/// 将药水替换为超ZUN啤酒。
/// </summary>
[HarmonyPatch(typeof(PotionCmd))]
public static class MamizouPotionPatch
{
    [HarmonyPatch(nameof(PotionCmd.TryToProcure), typeof(PotionModel), typeof(Player), typeof(int))]
    [HarmonyPrefix]
    public static bool TryToProcure_Prefix(ref PotionModel potion, Player player, int slotIndex,
        ref Task<PotionProcureResult> __result)
    {
        // 药水已经是超ZUN啤酒则无需替换
        if (potion is SuperZunBeerPotion) return true;

        // 检查玩家是否持有鲵吞亭特调
        var hasBlend = player.Relics.OfType<NidomiteiSpecialBlend>().Any();
        if (!hasBlend) return true;

        // 替换为超ZUN啤酒
        var beerPotion = ModelDb.Potion<SuperZunBeerPotion>().ToMutable();
        __result = PotionCmd.TryToProcure(beerPotion, player, slotIndex);
        return false; // 跳过原方法
    }
}
