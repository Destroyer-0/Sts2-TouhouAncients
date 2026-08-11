using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 将本项目帧动画怪物的受击触发直接交给怪物基类处理。
/// 这样既能按怪物状态禁止 hurt，也不会被同场景中的 AnimationPlayer 抢占。
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
internal static class TouhouAncientMonsterAnimationPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool BeforeSetAnimationTrigger(NCreature __instance, string trigger)
    {
        if (trigger != "Hit"
            || __instance.HasSpineAnimation
            || __instance.Entity.Monster is not TouhouAncientMonsterBase monster)
        {
            return true;
        }

        monster.HandleHitAnimationTrigger();
        return false;
    }
}
