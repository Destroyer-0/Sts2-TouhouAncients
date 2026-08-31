using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using TouhouAncients.Scripts.potions;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 鲵吞亭特调的 Harmony 补丁：若玩家持有鲵吞亭特调，
/// 将获取的药水（含战斗奖励界面的药水奖励）替换为超ZUN啤酒。
/// </summary>
[HarmonyPatch]
public static class MamizouPotionPatch
{
    /// <summary>
    /// 在领取药水时，若玩家持有鲵吞亭特调，将药水替换为超ZUN啤酒。
    /// </summary>
    [HarmonyPatch(typeof(PotionCmd), nameof(PotionCmd.TryToProcure),
        typeof(PotionModel), typeof(Player), typeof(int))]
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

    /// <summary>
    /// 在药水奖励生成时，若玩家持有鲵吞亭特调，
    /// 将奖励界面上显示的药水替换为超ZUN啤酒。
    /// </summary>
    [HarmonyPatch(typeof(PotionReward), nameof(PotionReward.Populate))]
    [HarmonyPostfix]
    public static void PotionReward_Populate_Postfix(PotionReward __instance)
    {
        // 检查玩家是否持有鲵吞亭特调
        var hasBlend = __instance.Player.Relics.OfType<NidomiteiSpecialBlend>().Any();
        if (!hasBlend) return;

        // 奖励已是超ZUN啤酒则无需替换
        if (__instance.Potion is SuperZunBeerPotion) return;

        // 替换奖励显示的药水为超ZUN啤酒
        var beerPotion = ModelDb.Potion<SuperZunBeerPotion>().ToMutable();
        Traverse.Create(__instance).Property(nameof(PotionReward.Potion)).SetValue(beerPotion);
    }
}
