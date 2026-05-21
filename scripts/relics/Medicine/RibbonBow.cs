using System.Collections.Generic;
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
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 丝带蝴蝶结：
/// 拾起时，从你的牌组选择2张非能力牌，为它们附魔"誓约"。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class RibbonBow : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("EnchantmentName", ModelDb.Enchantment<Oath>().Title.GetFormattedText()),
        new DynamicVar("SelectCount", 2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Oath>();

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        // 从牌组中选择 SelectCount 张非能力牌
        
        CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 2);
        Oath recollection = ModelDb.Enchantment<Oath>();
        foreach (CardModel item in await CardSelectCmd.FromDeckForEnchantment(base.Owner, recollection, 2, prefs))
        {
            CardCmd.Enchant(recollection.ToMutable(), item, 1m);
            CardCmd.Preview(item);
        }
    }
}
