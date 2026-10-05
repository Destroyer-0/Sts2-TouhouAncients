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

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 启示之梦：拾起时，从三张梦境牌中选择一张加入牌组（该牌自带休息时的「梦醒时」变化效果）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class RevelationDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<IllusoryDreamWhisper>(),
        HoverTipFactory.FromCard<DeepDreamSlumber>(),
        HoverTipFactory.FromCard<EndlessNightmare>()
    ];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var options = new List<CardModel>
        {
            base.Owner.RunState.CreateCard<IllusoryDreamWhisper>(base.Owner),
            base.Owner.RunState.CreateCard<DeepDreamSlumber>(base.Owner),
            base.Owner.RunState.CreateCard<EndlessNightmare>(base.Owner),
        };

        var picked = await CardSelectCmd.FromChooseACardScreen(
            new BlockingPlayerChoiceContext(), options, base.Owner);
        if (picked == null) return;

        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(picked, PileType.Deck), 2f);
    }
}
