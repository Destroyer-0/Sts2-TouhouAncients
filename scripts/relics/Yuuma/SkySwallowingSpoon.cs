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

    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(8)];
    
    
    // 阻止诅咒加入牌组
    public override bool ShouldAddToDeck(CardModel card)
    {
        if (card.Owner != base.Owner) return true;
        TouhouAncients_SwallowedCards++;
        Flash();
        CreatureCmd.GainMaxHp(card.Owner.Creature, base.DynamicVars["MaxHp"].IntValue);
        return false;
        return true;
    }
}
