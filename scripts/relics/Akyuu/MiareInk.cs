using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 御阿礼·墨：拾起时，为你牌组中所有除打击与防御以外的初始牌附魔稳定（原版附魔：保留）。
/// 没有这类牌时本遗物不出现在选项里。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MiareInk : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Steady>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("EnchantmentName", ModelDb.Enchantment<Steady>().Title.GetFormattedText())
    ];

    public override bool HasUponPickupEffect => true;

    public override bool CanAppear(Player? player) => player?.Creature != null && GetTargets(player).Any();

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        var targets = GetTargets(player).ToList();
        if (targets.Count == 0) return;

        Flash();
        foreach (var card in targets)
        {
            CardCmd.Enchant<Steady>(card, 1m);
        }
        CardCmd.Preview(targets, 2f);
    }

    /// <summary>牌组里的初始牌（Basic）中除打击、防御以外那些可以被附魔的。</summary>
    private static IEnumerable<CardModel> GetTargets(Player player)
    {
        var steady = ModelDb.Enchantment<Steady>();
        return PileType.Deck.GetPile(player).Cards.Where(c =>
            c.Rarity == CardRarity.Basic
            && !c.Tags.Contains(CardTag.Strike)
            && !c.Tags.Contains(CardTag.Defend)
            && steady.CanEnchant(c));
    }
}
