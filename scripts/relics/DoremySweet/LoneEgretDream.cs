using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 孤鹭之梦：拾起时，从 3 张带「缄口」附魔的无色牌中选一张加入牌组（可跳过）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class LoneEgretDream : TouhouAncientRelics
{
    private const int OfferCount = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(OfferCount)];

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [..HoverTipFactory.FromEnchantment<Taciturn>()];

    public override async Task AfterObtained()
    {
        Player? player = base.Owner;
        if (player?.Creature == null) return;

        var context = new ThrowingPlayerChoiceContext();

        List<CardModel> offers = BuildOffers(player);
        if (offers.Count == 0) return;

        Flash();
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(context, offers, player, canSkip: true);
        if (selected == null) return;

        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(selected, PileType.Deck), 2f);
    }

    /// <summary>3 张互不相同、已附魔「缄口」的无色牌。</summary>
    private static List<CardModel> BuildOffers(Player player)
    {
        Taciturn enchantment = ModelDb.Enchantment<Taciturn>();
        List<CardModel> pool = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.IsAllowedInRun(player.RunState) && enchantment.CanEnchant(c))
            .ToList();

        var offers = new List<CardModel>(OfferCount);
        foreach (CardModel model in pool.UnstableShuffle(player.PlayerRng.Rewards))
        {
            if (offers.Count >= OfferCount) break;
            offers.Add(player.RunState.CreateCard(model, player));
        }

        foreach (CardModel card in offers) CardCmd.Enchant<Taciturn>(card, 1m);
        return offers;
    }
}
