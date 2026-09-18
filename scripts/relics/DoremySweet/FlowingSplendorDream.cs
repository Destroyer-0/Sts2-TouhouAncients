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

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 流绮之梦：拾起时，为牌组中的一对打击和防御附魔：华彩。
///
/// 选取方式与「涅奥的护符」一致：只在牌组中的基础牌（Rarity == Basic）里查找，
/// 分别取最后一张打击与最后一张防御，直接对它们就地附魔，不新增牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class FlowingSplendorDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [..HoverTipFactory.FromEnchantment<Glam>()];

    public override bool HasUponPickupEffect => true;

    /// <summary>选项条件：能凑齐一对可附魔「华彩」的基础打击与防御，否则不出现。</summary>
    public override bool CanAppear(Player? player)
    {
        if (player?.Creature == null) return false;

        Glam glam = ModelDb.Enchantment<Glam>();
        List<CardModel> basics = PileType.Deck.GetPile(player).Cards
            .Where(c => c.Rarity == CardRarity.Basic)
            .ToList();

        bool hasStrike = basics.Any(c => c.Tags.Contains(CardTag.Strike) && glam.CanEnchant(c));
        bool hasDefend = basics.Any(c => c.Tags.Contains(CardTag.Defend) && glam.CanEnchant(c));

        return hasStrike && hasDefend;
    }

    public override Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return Task.CompletedTask;

        var basics = PileType.Deck.GetPile(player).Cards
            .Where(c => c.Rarity == CardRarity.Basic)
            .ToList();

        CardModel? strike = basics.LastOrDefault(c => c.Tags.Contains(CardTag.Strike));
        CardModel? defend = basics.LastOrDefault(c => c.Tags.Contains(CardTag.Defend));

        // 角色没有标准打击/防御时不做任何事（例如特殊角色）
        if (strike == null && defend == null) return Task.CompletedTask;

        Flash();

        foreach (var card in new[] { strike, defend })
        {
            if (card == null) continue;

            CardCmd.Enchant<Glam>(card, 1m);
            CardCmd.Preview(card);
        }

        return Task.CompletedTask;
    }
}
