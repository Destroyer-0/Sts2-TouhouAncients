using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 蘑菇便当盒：在每个回合开始时获得1能量。回合结束时，如果你的手牌数大于等于3，
/// 将一张孢子心灵（SporeMind）加入抽牌堆。
/// 彩蛋：替换你所有的遗物：面包
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MushroomBento : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new CardsVar(3)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<SporeMind>().Append(HoverTipFactory.ForEnergy(this));

    public static HashSet<Player> hasReplacedStart = new();

    public override Task AfterObtained()
    {
        if (hasReplacedStart.Add(base.Owner))
        {
            var breads = new List<RelicModel>(Owner.Relics.Where(x => x is Bread));
            foreach (var bread in breads)
            {
                RelicCmd.Replace(bread, ModelDb.Relic<MushroomBento>().ToMutable());
            }
        }

        hasReplacedStart.Remove(base.Owner);
        return base.AfterObtained();
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Creature.Side) return;
        if (base.Owner.PlayerCombatState == null) return;

        var hand = PileType.Hand.GetPile(base.Owner);
        if (hand.Cards.Count < 3) return;

        Flash();
        await CardPileCmd.AddToCombatAndPreview<SporeMind>(base.Owner.Creature, PileType.Draw, 1, true);
    }
}

/// <summary>
/// 持有 MushroomBento 时，在 BigMushroom 悬浮提示中追加蘑菇便当说明
/// </summary>
[HarmonyPatch]
public static class BigMushroomHoverTipPatch
{
    private static MethodBase TargetMethod()
    {
        return typeof(BigMushroom).GetMethod("get_HoverTips");
    }

    [HarmonyPostfix]
    public static void Postfix(BigMushroom __instance, ref IEnumerable<IHoverTip> __result)
    {
        try
        {
            if (__instance.Owner?.GetRelic<MushroomBento>() != null && __result is List<IHoverTip> list)
            {
                list.Add(new HoverTip(
                    new LocString("relics", "TOUHOUANCIENTS-MUSHROOM_BENTO.title"),
                    new LocString("relics", "TOUHOUANCIENTS-MUSHROOM_BENTO.mushroom")
                ));
            }
        }
        catch (System.Exception e)
        {
            Log.Error(e.ToString());
        }
    }
}

/// <summary>
/// 持有 MushroomBento 时，BigMushroom 减少抽牌效果不生效
/// </summary>
[HarmonyPatch]
public static class BigMushroomDrawPatch
{
    private static MethodBase TargetMethod()
    {
        return typeof(BigMushroom).GetMethod("ModifyHandDraw", new[]
        {
            typeof(Player),
            typeof(decimal)
        });
    }

    [HarmonyPrefix]
    public static bool Prefix(BigMushroom __instance, ref decimal __result, Player player, decimal cardsToDraw)
    {
        try
        {
            if (player?.GetRelic<MushroomBento>() != null)
            {
                __result = cardsToDraw;
                return false;
            }
        }
        catch (System.Exception e)
        {
            Log.Error(e.ToString());
        }

        return true;
    }
}

/// <summary>
/// 持有 MushroomBento 时，在 FragrantMushroom 悬浮提示中追加蘑菇便当说明
/// </summary>
[HarmonyPatch]
public static class FragrantMushroomHoverTipPatch
{
    private static MethodBase TargetMethod()
    {
        return typeof(FragrantMushroom).GetMethod("get_HoverTips");
    }

    [HarmonyPostfix]
    public static void Postfix(FragrantMushroom __instance, ref IEnumerable<IHoverTip> __result)
    {
        try
        {
            if (__instance.Owner?.GetRelic<MushroomBento>() != null && __result is List<IHoverTip> list)
            {
                list.Add(new HoverTip(
                    new LocString("relics", "TOUHOUANCIENTS-MUSHROOM_BENTO.title"),
                    new LocString("relics", "TOUHOUANCIENTS-MUSHROOM_BENTO.mushroom")
                ));
            }
        }
        catch (System.Exception e)
        {
            Log.Error(e.ToString());
        }
    }
}

/// <summary>
/// 持有 MushroomBento 时，FragrantMushroom 拾起时失去生命效果失效（升级效果保留）
/// </summary>
[HarmonyPatch]
public static class FragrantMushroomHpLossPatch
{
    private static MethodBase TargetMethod()
    {
        return typeof(FragrantMushroom).GetMethod("AfterObtained");
    }

    [HarmonyPrefix]
    public static bool Prefix(FragrantMushroom __instance)
    {
        try
        {
            var player = __instance.Owner;
            if (player?.GetRelic<MushroomBento>() != null)
            {
                // 跳过 CreatureCmd.Damage，仅执行升级部分
                var upgradable = PileType.Deck.GetPile(player).Cards
                    .Where(c => c?.IsUpgradable ?? false)
                    .ToList()
                    .StableShuffle(player.RunState.Rng.Niche)
                    .Take(__instance.DynamicVars.Cards.IntValue);
                foreach (var card in upgradable)
                {
                    CardCmd.Upgrade(card, CardPreviewStyle.MessyLayout);
                }
                return false; // 跳过原方法（不执行扣血）
            }
        }
        catch (System.Exception e)
        {
            Log.Error(e.ToString());
        }
        return true;
    }
}