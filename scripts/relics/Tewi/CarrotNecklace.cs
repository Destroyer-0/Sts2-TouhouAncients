using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 萝卜项链：拾起时移除三张牌，商人移除卡牌服务价格翻倍。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class CarrotNecklace : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 1, 3);
        var selected = (await CardSelectCmd.FromDeckGeneric(base.Owner, prefs)).ToList();

        if (selected.Count > 0)
        {
            Flash();
            await CardPileCmd.RemoveFromDeck(selected);
        }
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
    {
        if (player != base.Owner) return cost;

        // 仅翻倍"移除卡牌"服务价格
        if (entry is MerchantCardRemovalEntry)
        {
            return cost * 2m;
        }
        return cost;
    }
}
