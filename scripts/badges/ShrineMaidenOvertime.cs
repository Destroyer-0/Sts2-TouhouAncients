using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.badges;

/// <summary>二重结界计数耗尽后，仍靠无视路线移动再次触发它「敌人生命变为1」。</summary>
public class ShrineMaidenOvertime : TouhouAncientBadge
{
    internal const string FlagId = "touhouancients-shrinemaidenovertime";

    public ShrineMaidenOvertime() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Gold;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) => BadgeFlags.Get(player, FlagId);

    public static void Unlock(Player player) => BadgeFlags.Set(player, FlagId);

    public static void RegisterSave() => BadgeFlags.Register(FlagId);
}

/// <summary>挂在遗物本身：置位时机与它真正把敌人生命设为1那一次完全一致，不复刻无视线判定。</summary>
[HarmonyPatch(typeof(DuplexBarrier), nameof(DuplexBarrier.BeforeCombatStart))]
internal static class ShrineMaidenOvertimePatch
{
    [HarmonyPostfix]
    private static void AfterBeforeCombatStart(DuplexBarrier __instance)
    {
        if (!__instance.IsUsedUp) return;
        if (!Traverse.Create(__instance).Field<bool>("_wasSkippedThisRoom").Value) return;

        ShrineMaidenOvertime.Unlock(__instance.Owner);
    }
}
