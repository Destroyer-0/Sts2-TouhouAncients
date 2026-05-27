using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 蛊毒魔盒：
/// 拾起时，将你牌组中的所有"打击"替换为蛇咬（SnakeBite）并附魔：蛊毒。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class MedicinePoisonBox : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Snakebite>().Concat(HoverTipFactory.FromEnchantment<MedicinePoison>(3));

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var deck = player.Deck;

        // 找出牌组中所有的"打击"牌
        var strikes = deck.Cards
            .Where(c => c.Tags.Contains(CardTag.Strike))
            .ToList();

        if (strikes.Count == 0) return;

        var addResults = new List<CardTransformation>();

        // 逐一替换为蛇咬并附魔蛊毒
        foreach (var strike in strikes)
        {
            addResults.Add(new CardTransformation(strike, CreateMaulFromOriginal(strike)));
        }

        await CardCmd.Transform(addResults, null, CardPreviewStyle.GridLayout);
    }
    
    private CardModel CreateMaulFromOriginal(CardModel original)
    {
        CardModel cardModel = (base.Owner.RunState.CreateCard<Snakebite>(base.Owner));
        if (original.IsUpgraded && cardModel.IsUpgradable)
        {
            CardCmd.Upgrade(cardModel);
        }

        EnchantmentModel enchantmentModel = ModelDb.Enchantment<MedicinePoison>().ToMutable();
        if (original.Enchantment != null)
        {
            if (enchantmentModel.CanEnchant(cardModel))
            {
                CardCmd.Enchant(enchantmentModel, cardModel, 3);
            }
        }
        else
        {
            CardCmd.Enchant(enchantmentModel, cardModel, 3);
        }

        return cardModel;
    }
}
