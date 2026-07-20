using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 凭依之灵：将 极奢形态 和 至贫形态 加入你的牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class PossessionSpirit : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<RichestForm>()
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<PoorestForm>());

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        var richestForm = player.RunState.CreateCard<RichestForm>(player);
        var poorestForm = player.RunState.CreateCard<PoorestForm>(player);
        var cardModelList = new List<CardModel>() { richestForm, poorestForm };
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(cardModelList, PileType.Deck), 2f);
    }
}