using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 火鼠的皮衣Patch：使持有该遗物的玩家可以打出灼伤。
/// </summary>
[HarmonyPatch]
public static class HinezumiNoKawagoromoPatchs
{
    private static MethodBase TargetMethod()
    {
        return typeof(CardModel).GetMethod("CanPlay", new[]
        {
            typeof(UnplayableReason).MakeByRefType(),
            typeof(AbstractModel).MakeByRefType()
        });
    }

    public static void Postfix(CardModel __instance, ref bool __result, ref UnplayableReason reason, ref AbstractModel preventer)
    {
        try
        {
            if (__instance.Owner != null && __instance.Owner.GetRelic<HinezumiNoKawagoromo>() != null
                && __instance is Burn)
            {
                if (!__result && reason.HasFlag(UnplayableReason.HasUnplayableKeyword))
                {
                    reason &= ~UnplayableReason.HasUnplayableKeyword;
                    if (reason == UnplayableReason.None)
                    {
                        __result = true;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }
}

/// <summary>
/// 火鼠的皮衣：在战斗开始时，将3张灼伤放入你的抽牌堆。
/// 你可以打出原本不能被打出的灼伤，抽一张牌、获得6格挡与1荆棘。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HinezumiNoKawagoromo : TouhouAncientRelics
{
    private const int BurnCount = 3;
    private const int DrawCount = 1;
    private const decimal BlockAmount = 6m;
    private const decimal ThornsAmount = 1m;

    public override bool HasUponPickupEffect => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BurnCount", BurnCount),
        new DynamicVar("Block", BlockAmount),
        new DynamicVar("Thorns", ThornsAmount),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ThornsPower>(),
    ];

    /// <summary>
    /// 战斗开始时，将3张灼伤放入抽牌堆。
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        var player = base.Owner;
        if (player == null) return;

        for (int i = 0; i < BurnCount; i++)
        {
            var burn = player.RunState.CreateCard(ModelDb.Card<Burn>(), player);
            await CardPileCmd.Add(burn, PileType.Draw);
        }
    }

    /// <summary>
    /// 打出灼伤时：抽一张牌、获得6格挡与1荆棘。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (cardPlay.Card is not Burn) return;

        Flash();
        await CardPileCmd.Draw(context, DrawCount, base.Owner);
        await CreatureCmd.GainBlock(base.Owner.Creature, BlockAmount, this);
        await PowerCmd.Apply<ThornsPower>(base.Owner.Creature, ThornsAmount, base.Owner.Creature, null);
    }
}
