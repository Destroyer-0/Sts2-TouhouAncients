using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

// /// <summary>
// /// 火鼠的皮衣Patch：使持有该遗物的玩家可以打出灼伤。
// /// </summary>
// [HarmonyPatch]
// public static class HinezumiNoKawagoromoPatchs
// {
//     private static MethodBase TargetMethod()
//     {
//         return typeof(CardModel).GetMethod("CanPlay", new[]
//         {
//             typeof(UnplayableReason).MakeByRefType(),
//             typeof(AbstractModel).MakeByRefType()
//         });
//     }
//
//     public static void Postfix(CardModel __instance, ref bool __result, ref UnplayableReason reason, ref AbstractModel preventer)
//     {
//         try
//         {
//             if (__instance.Owner != null && __instance.Owner.GetRelic<HinezumiNoKawagoromo>() != null
//                                          && __instance is Burn)
//             {
//                 if (!__result && reason.HasFlag(UnplayableReason.HasUnplayableKeyword))
//                 {
//                     reason &= ~UnplayableReason.HasUnplayableKeyword;
//                     if (reason == UnplayableReason.None)
//                     {
//                         __result = true;
//                     }
//                 }
//             }
//         }
//         catch (Exception e)
//         {
//             Log.Error(e.ToString());
//         }
//     }
// }

/// <summary>
/// 火鼠的皮衣：在战斗开始时，将3张灼伤放入你的抽牌堆。
/// 你可以打出原本不能被打出的灼伤，抽一张牌、获得6格挡与1荆棘。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HinezumiNoKawagoromo : TouhouAncientRelics
{
    private const int DrawCount = 1;
    private const int BlockAmount = 9;
    private const decimal ThornsAmount = 2m;

    public override bool HasUponPickupEffect => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BurnCount", 3m),
        new BlockVar(BlockAmount, ValueProp.Move),
        new DynamicVar("Thorns", ThornsAmount),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Burn>().Append(
            HoverTipFactory.FromPower<ThornsPower>());

    /// <summary>
    /// 战斗开始时，将3张灼伤放入抽牌堆。
    /// </summary>
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player == base.Owner && combatState.RoundNumber == 1)
        {
            Flash();
            List<CardModel> list = new List<CardModel>();
            for (int i = 0; i < base.DynamicVars["BurnCount"].IntValue; i++)
            {
                list.Add(combatState.CreateCard<Burn>(base.Owner));
            }

            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Draw, addedByPlayer: true, CardPilePosition.Random));
            await Cmd.Wait(2f);
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
        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<ThornsPower>(base.Owner.Creature, ThornsAmount, base.Owner.Creature, null);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner.Creature) return;
        if (cardSource is Burn)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block, null);
            await PowerCmd.Apply<ThornsPower>(base.Owner.Creature, ThornsAmount, base.Owner.Creature, null);
            await PowerCmd.Apply<DrawCardsNextTurnPower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
        }
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner.Creature) return base.ModifyDamageAdditive(target, amount, props, dealer, cardSource);
        if (cardSource is Burn)
        {
            return -10000;
        }
        return base.ModifyDamageAdditive(target, amount, props, dealer, cardSource);
    }

    // public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    // {
    //     if (target != base.Owner.Creature) return Task.CompletedTask;
    //     if (cardSource is Burn)
    //     {
    //         amount = 0;
    //     }
    //
    //     return Task.CompletedTask;
    // }
}