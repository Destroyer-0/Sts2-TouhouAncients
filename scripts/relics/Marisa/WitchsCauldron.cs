using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 魔女的炼药锅：拾起时获得2个药水栏位。你可以在休息处炼药。
/// 炼药：获得3瓶药。变化至多一张牌。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class WitchsCauldron : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(
            new LocString("rest_site_ui", "OPTION_WITCHS_CAULDRON.name"),
            new LocString("rest_site_ui", "OPTION_WITCHS_CAULDRON.description"))
    ];

    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained()
    {
        // 获得2个药水栏位
        base.Owner.AddToMaxPotionCount(2);
        return Task.CompletedTask;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != base.Owner) return false;
        options.Add(new WitchsCauldronRestSiteOption(player, this));
        return true;
    }

    internal async Task Brew()
    {
        Flash();

        var potionList = new List<PotionModel>();
        // 获得3瓶随机药水
        for (int i = 0; i < 3; i++)
        {
            var potion = PotionFactory.CreateRandomPotionOutOfCombat(base.Owner, base.Owner.PlayerRng.Rewards);
            potionList.Add(potion.ToMutable());
        }
        var rewards = potionList.Select(r => new PotionReward(r, base.Owner)).ToList<Reward>();
        await new RewardsSet(base.Owner).WithCustomRewards(rewards).Offer();
        // 变化至多一张牌
        var prefs = new CardSelectorPrefs(new LocString("card_selection", "TO_TRANSFORM"), 0, 1);
        var selected = (await CardSelectCmd.FromDeckForTransformation(base.Owner, prefs)).ToList();

        if (selected.Count <= 0)
        {
            return;
        }
        
        foreach (var card in selected)
        {
            var newCard = CardFactory.CreateRandomCardForTransform(card, isInCombat: false, base.Owner.PlayerRng.Transformations);
            await CardCmd.Transform(card, newCard);
            CardCmd.Preview(newCard);
        }
    }
}

public class WitchsCauldronRestSiteOption : RestSiteOption
{
    private readonly WitchsCauldron _relic;

    public override string OptionId => "WITCHS_CAULDRON";

    public override LocString Description => new LocString("rest_site_ui", "OPTION_WITCHS_CAULDRON.description");

    public WitchsCauldronRestSiteOption(Player owner, WitchsCauldron relic) : base(owner)
    {
        _relic = relic;
    }

    public override async Task<bool> OnSelect()
    {
        await _relic.Brew();
        return true;
    }
}
