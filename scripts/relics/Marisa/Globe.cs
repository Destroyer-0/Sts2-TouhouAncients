using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 地球仪：以保留升级的状态变化5张牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class Globe : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(5)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Transform)];
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        List<CardModel> list = (await CardSelectCmd.FromDeckForTransformation(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, base.DynamicVars.Cards.IntValue),
            player: base.Owner)).ToList();
        foreach (CardModel item in list)
        {
            var shouldUpgrade = item.IsUpgraded;
            CardModel cardModel =
                CardFactory.CreateRandomCardForTransform(item, isInCombat: false, base.Owner.PlayerRng.Transformations);
            if (shouldUpgrade)
            {
                CardCmd.Upgrade(cardModel);
            }

            await CardCmd.Transform(item, cardModel);
        }
    }
}