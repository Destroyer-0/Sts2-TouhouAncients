using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 香奈儿手包：升级商店内出售的卡牌，并为这些牌附魔：华彩。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class ChanelHandbag : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<Glam>();

    public override void ModifyMerchantCardCreationResults(Player player, List<CardCreationResult> cards)
    {
        if(player!=Owner)return;
        var glam = ModelDb.Enchantment<Glam>();
        foreach (var card in cards)
        {
            if (card.Card.IsUpgradable)
            {
                CardCmd.Upgrade(card.Card);
            }

            if (glam.CanEnchant(card.Card))
            {
                CardCmd.Enchant<Glam>(card.Card, 1);
            }
        }

        base.ModifyMerchantCardCreationResults(player, cards);
    }
}
