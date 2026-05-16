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
/// 白旗：拾起时，从牌组中选择一张耗能不为X的牌，为它附魔：诈术。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class WhiteFlag : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<Trick>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("EnchantmentName", ModelDb.Enchantment<Trick>().Title.GetFormattedText())
    ];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var enchantment = ModelDb.Enchantment<Trick>();

        var selected = (await CardSelectCmd.FromDeckForEnchantment(
            player,
            enchantment,
            1,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1)
        )).FirstOrDefault();

        if (selected != null)
        {
            CardCmd.Enchant<Trick>(selected, 1m);
            CardCmd.Preview(selected);
        }
    }
}
