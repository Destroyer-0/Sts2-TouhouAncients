using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 博丽御币：拾起时，选择一张卡，为其附魔：降伏。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HakureiGohei : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("EnchantmentName", ModelDb.Enchantment<Exorcism>().Title.GetFormattedText())]; 
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Exorcism>();

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var enchantment = ModelDb.Enchantment<Exorcism>();

        var selected = (await CardSelectCmd.FromDeckForEnchantment(
            player,
            enchantment,
            1,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1)
        )).FirstOrDefault();

        if (selected != null)
        {
            CardCmd.Enchant(enchantment.ToMutable(), selected, 1m);
            var vfx = NCardEnchantVfx.Create(selected);
            if (vfx != null)
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
        }
    }
}
