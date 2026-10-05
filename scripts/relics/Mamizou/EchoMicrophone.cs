using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
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
/// 回音话筒：拾起时，选择至多 3 张牌，附魔：回响。
/// 回响：打出此牌后，将一张本回合费用 +1 的复制品加入你的手牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class EchoMicrophone : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3),
        new StringVar("EnchantmentName", ModelDb.Enchantment<Echo>().Title.GetFormattedText())
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Echo>();

    public override bool HasUponPickupEffect => true;

    /// <summary>选项条件：牌组里至少有一张可附魔「回响」的牌，否则不出现。</summary>
    public override bool CanAppear(Player? player)
    {
        if (player?.Creature == null) return false;

        Echo echo = ModelDb.Enchantment<Echo>();
        return player.Deck.Cards.Any(echo.CanEnchant);
    }

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var enchantment = ModelDb.Enchantment<Echo>();
        var maxCards = DynamicVars["Cards"].IntValue;

        var selected = (await CardSelectCmd.FromDeckForEnchantment(
            player,
            enchantment,
            maxCards,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, maxCards)
        )).ToList();

        foreach (var card in selected)
        {
            CardCmd.Enchant<Echo>(card, 1m);
            var vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
        }
    }
}
