using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 烤味噌：升级7张牌。战斗奖励与商店中的卡牌不再出现升级后的。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class GrilledMiso : TouhouAncientRelics
{
    private const int CardsToUpgrade = 7;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(7)];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        List<CardModel> list = (await CardSelectCmd.FromDeckForUpgrade(prefs: new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, base.DynamicVars.Cards.IntValue), player: base.Owner)).ToList();
        foreach (CardModel item in list)
        {
            CardCmd.Upgrade(item);
        }
    }

    /// <summary>
    /// 修改卡牌奖励升级概率为0（不出现升级后的卡牌）。
    /// </summary>
    public override decimal ModifyCardRewardUpgradeOdds(Player player, CardModel card, decimal odds)
    {
        if (player != base.Owner) return odds;
        return 0m;
    }

    /// <summary>
    /// 修改奖励选项，强制所有生成的卡牌为未升级状态。
    /// </summary>
    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions options)
    {
        if (player != base.Owner) return false;
        options.WithFlags(options.Flags | CardCreationFlags.NoUpgradeRoll);
        return true;
    }
}
