using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 记忆烧瓶：选择一张牌，为其附魔：回忆。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class MemoryFlask : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StringVar("EnchantmentName", ModelDb.Enchantment<Recollection>().Title.GetFormattedText())];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<Recollection>();

    public override bool HasUponPickupEffect => true;

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
