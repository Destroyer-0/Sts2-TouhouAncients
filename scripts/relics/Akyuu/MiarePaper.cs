using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 御阿礼·纸：拾起时，获得1组升级过的卡牌奖励。
/// 牌要先升好再交给 CardReward（用「手动指定牌表」的重载），否则奖励面板里看到的还是未升级的牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MiarePaper : TouhouAncientRelics
{
    private const int CardCount = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        var options = CardCreationOptions.ForNonCombatWithDefaultOdds([player.Character.CardPool]);
        var results = CardFactory.CreateForReward(player, CardCount, options).ToList();
        if (results.Count == 0) return;

        foreach (var result in results)
        {
            if (result.Card.IsUpgradable && !result.Card.IsUpgraded)
            {
                CardCmd.Upgrade(result.Card, CardPreviewStyle.None);
            }
        }

        Flash();
        var reward = new CardReward(results.Select(r => r.Card), CardCreationSource.Other, player, options);
        await RewardsCmd.OfferCustom(player, [reward]);
    }
}
