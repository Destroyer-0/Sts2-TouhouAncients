using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TouhouAncients.Scripts.Vfx;

namespace TouhouAncients.Scripts.Patches;

[HarmonyPatch(typeof(NEventRoom), "_Ready")]
public static class AncientEntrancePatch
{
    static readonly string[] Types = { "HakureiReimuAncient", "HinanawiTenshiAncient", "HouraisanKaguyaAncient", "InabaTewiAncient", "JunkoAncient", "KijinSeijaAncient", "KirisameMarisaAncient", "KomejiSatoriAncient", "KotiyaSanaeAncient", "MedicineMelancholyAncient", "RemiliaScarletAncient", "SaigyoujiYuyukoAncient", "ToutetsuYuumaAncient", "WatariNinaAncient", "YorigamiSisterAncient", "FutatsuiwaMamizouAncient" };
    static void Postfix(NEventRoom __instance, EventModel ____event, bool ____isPreFinished)
    {
        if (____isPreFinished || ____event is not AncientEventModel || ____event.GetType().Namespace != "TouhouAncients.Scripts") return;

        int style = ____event is IdleKoishiAncient ? 17 : System.Array.IndexOf(Types, ____event.GetType().Name); if (style < 0) return;
        // Match the selected ancient even when a trainer opens its room in another act.
        Callable.From(() => { if (GodotObject.IsInstanceValid(__instance) && __instance.IsInsideTree()) NAncientEntranceTransition.Play(__instance, style); }).CallDeferred();
    }
}
