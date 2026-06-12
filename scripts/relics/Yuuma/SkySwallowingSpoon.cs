using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 吞天之勺：向牌组中加入卡牌时，吞噬之（不加入牌组）并获得4最大生命。
/// 通过 Harmony Patch 在卡牌加入牌组后立即移除。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SkySwallowingSpoon : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";
    [SavedProperty]
    public int TouhouAncients_SwallowedCards
    {
        get => swallowedCards;
        set
        {
            AssertMutable();
            swallowedCards = value;
            InvokeDisplayAmountChanged();
        }
    }

    private int swallowedCards;

    public override bool HasUponPickupEffect => false;
    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_SwallowedCards;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(4)];
}

/// <summary>
/// 吞天之勺 Harmony 补丁
/// </summary>
[HarmonyPatch]
public static class SkySwallowingSpoonPatchs
{
    private static MethodBase TargetMethod()
    {
        return typeof(CardPileCmd).GetMethod("Add", new[]
        {
            typeof(CardModel),
            typeof(PileType),
            typeof(CardPilePosition),
            typeof(AbstractModel),
            typeof(bool)
        });
    }

    public static async void Postfix(CardModel card, PileType newPileType, Task<CardPileAddResult> __result)
    {
        try
        {
            if (newPileType != PileType.Deck) return;
            if (card?.Owner == null) return;

            var spoon = card.Owner.GetRelic<SkySwallowingSpoon>();
            if (spoon == null) return;

            var addResult = await __result;
            if (!addResult.success) return;

            // 立即从牌组中移除
            await CardPileCmd.RemoveFromDeck(card, showPreview: false);
            spoon.TouhouAncients_SwallowedCards++;
            spoon.Flash();
            await CreatureCmd.GainMaxHp(card.Owner.Creature, spoon.DynamicVars["MaxHp"].IntValue);
        }
        catch (System.Exception e)
        {
            Log.Error(e.ToString());
        }
    }
}
