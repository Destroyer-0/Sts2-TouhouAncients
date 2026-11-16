using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
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
        var options1 = new CardCreationOptions([ModelDb.CardPool<ColorlessCardPool>()], CardCreationSource.Other, CardRarityOddsType.RegularEncounter);
        List<CardModel> options = CardFactory.CreateForReward(Owner, 3, options1).Select((Func<CardCreationResult, CardModel>) (c => c.Card)).ToList<CardModel>();
        foreach (CardModel card in options) CardCmd.Enchant<Taciturn>(card, 1m);
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), options, Owner, canSkip: true);
        if (selected == null) return;

        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(selected, PileType.Deck), 2f);
        foreach (CardModel card in options)
        {
            if (card != selected)
                Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(Owner.NetId).CardChoices.Add(new CardChoiceHistoryEntry(card, false));
        }
    }
}
