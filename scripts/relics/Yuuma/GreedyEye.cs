using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Enchantment;
using Greed = TouhouAncients.Scripts.Enchantment.Greed;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 贪婪之瞳：拾起时，获得两张带有贪欲附魔的未掘宝石。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class GreedyEye : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Cards", 2),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<Greed>().Concat(HoverTipFactory.FromCardWithCardHoverTips<HiddenGem>());
        

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        for (int i = 0; i < DynamicVars["Cards"].IntValue; i++)
        {
            var gem = player.RunState.CreateCard<HiddenGem>(player);
            CardCmd.Enchant<Greed>(gem, 1m);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(gem, PileType.Deck));
        }
    }
}
