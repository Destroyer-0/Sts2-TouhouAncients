using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class GiftFromMountain : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(300)
    ];

    public override async Task AfterObtained()
    {
        await PlayerCmd.GainGold(base.DynamicVars.Gold.BaseValue, base.Owner);
        var rewards = new List<Reward>();
        foreach (var rarity in new[] { CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare })
        {
            var options = CardCreationOptions
                .ForNonCombatWithUniformOdds(
                    new List<CardPoolModel> { base.Owner.Character.CardPool },
                    (CardModel c) => c.Rarity == rarity)
                .WithFlags(CardCreationFlags.NoRarityModification);

            rewards.Add(new CardReward(options, 3, base.Owner));
        }

        await RewardsCmd.OfferCustom(base.Owner, rewards);
    }
}
