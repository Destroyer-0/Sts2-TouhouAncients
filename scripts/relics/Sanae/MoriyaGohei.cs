using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class MoriyaGohei : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("EnchantmentName", ModelDb.Enchantment<Miracle>().Title.GetFormattedText())]; 
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Miracle>();
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var miracle = ModelDb.Enchantment<Miracle>();
        var deckCards = PileType.Deck.GetPile(base.Owner)
            .Cards
            .Where(c => miracle.CanEnchant(c))
            .ToList();

        var selectedCard = (await CardSelectCmd.FromDeckForEnchantment(
            base.Owner,
            miracle,
            1,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1)
        )).FirstOrDefault();

        if (selectedCard != null)
        {
            CardCmd.Enchant<Miracle>(selectedCard, 1m);

            var vfx = NCardEnchantVfx.Create(selectedCard);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }
    }
}
