using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;


[HarmonyPatch]
public static class CurseBreakerQiPatchs
{
    private static MethodBase TargetMethod()
    {
        return
            typeof(CardModel).GetMethod("CanPlay", new[] {
                typeof(UnplayableReason).MakeByRefType(),
                typeof(AbstractModel).MakeByRefType()
            });
    }

    public static void Postfix(CardModel __instance, ref bool __result, ref UnplayableReason reason, ref AbstractModel preventer)
    {
        try
        {
            if (__instance.Owner != null && __instance.Owner.GetRelic<CurseBreakerQi>() != null && __instance.Type == CardType.Curse)
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
/// 破厄真气：拾起时获得9张随机诅咒。可以打出诅咒牌，打出诅咒时获得1力量、1能量并抽2张牌。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class CurseBreakerQi : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Unplayable),
        HoverTipFactory.ForEnergy(this),
    ];
    
    public override async Task AfterObtained()
    {
        var player = base.Owner;

        // 获得9张随机诅咒
        HashSet<CardModel> availableCurses = (from c in ModelDb.CardPool<CurseCardPool>().GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint)
            where c.CanBeGeneratedByModifiers
            select c).ToHashSet();
        var rng = player.RunState.Rng.Shuffle;
        var cursesToAdd = availableCurses.ToList().UnstableShuffle(rng).Take(9);
        await CardPileCmd.AddCursesToDeck(cursesToAdd, player);
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Type is CardType.Curse)
        {
            card.RemoveKeyword(CardKeyword.Unplayable);
        }
        return base.AfterCardEnteredCombat(card);
    }

    // 打出诅咒时获得奖励
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (cardPlay.Card.Type != CardType.Curse) return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
        await PlayerCmd.GainEnergy(1, base.Owner);
        await CardPileCmd.Draw(context, 2, base.Owner);
    }
}
