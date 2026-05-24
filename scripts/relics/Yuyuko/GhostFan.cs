using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
/// 幽灵折扇：拾起时，选择至多三张技能牌，附魔：彷徨。
/// 彷徨：如果打出的上一张牌是攻击牌，则打出后回到你的手中并获得10格挡。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class GhostFan : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("EnchantmentName", ModelDb.Enchantment<Wandering>().Title.GetFormattedText())
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Wandering>();

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var enchantment = ModelDb.Enchantment<Wandering>();

        var selected = (await CardSelectCmd.FromDeckForEnchantment(
            player,
            enchantment,
            3,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 3)
        )).ToList();

        foreach (var card in selected)
        {
            CardCmd.Enchant<Wandering>(card, 1m);
            var vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
        }
    }
}
