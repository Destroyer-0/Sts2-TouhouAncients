using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 掌上的纯光：拾起时把初始打击/防御附魔纯粹。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class PareLightOfThePalm : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<PureLight>(1);

    /// <summary>牌组里还有初始打击/防御才作为选项出现，否则本遗物毫无意义。</summary>
    public override bool CanAppear(Player? player)
    {
        if (player?.Creature == null) return false;

        return player.Deck.Cards.Any(IsBasicStrikeOrDefend);
    }

    public override Task AfterObtained()
    {
        var player = base.Owner;
        if (player == null) return Task.CompletedTask;

        foreach (var card in player.Deck.Cards.ToList())
        {
            if (!IsBasicStrikeOrDefend(card)) continue;

            if (card.Enchantment != null) CardCmd.ClearEnchantment(card);
            CardCmd.Enchant<PureLight>(card, 1m);
            NCardEnchantVfx child = NCardEnchantVfx.Create(card);
            if (child != null)
            {
                NRun instance = NRun.Instance;
                if (instance != null)
                    instance.GlobalUi.CardPreviewContainer.AddChildSafely((Node) child);
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsBasicStrikeOrDefend(CardModel card)
    {
        return card.Rarity == CardRarity.Basic
               && (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend));
    }
}
