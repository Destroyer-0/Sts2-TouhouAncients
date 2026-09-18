using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 记忆烧瓶：选择一张牌，为其附魔：回忆。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MemoryFlask : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StringVar("EnchantmentName", ModelDb.Enchantment<Recollection>().Title.GetFormattedText())];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<Recollection>();

    public override bool HasUponPickupEffect => true;

    /// <summary>选项条件：牌组里至少有一张可附魔「回忆」的牌，否则不出现。</summary>
    public override bool CanAppear(Player? player)
    {
        if (player?.Creature == null) return false;

        Recollection recollection = ModelDb.Enchantment<Recollection>();
        return player.Deck.Cards.Any(c => recollection.CanEnchant(c));
    }

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
        Recollection recollection = ModelDb.Enchantment<Recollection>();
        foreach (CardModel item in await CardSelectCmd.FromDeckForEnchantment(base.Owner, recollection, 1, prefs))
        {
            CardCmd.Enchant(recollection.ToMutable(), item, 1m);
            CardCmd.Preview(item);
        }
    }
}
