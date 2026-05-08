using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 地狱鸦羽：每当你洗牌时，将一张灼伤置入手牌，并获得2力量。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HellOkuu : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Strength", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Burn>().Append(HoverTipFactory.FromPower<StrengthPower>());

    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler != base.Owner) return;

        Flash();

        // 将一张灼伤置入手牌
        var burn = base.Owner.RunState.CreateCard(ModelDb.Card<Burn>(), base.Owner);
        await CardPileCmd.Add(burn, PileType.Hand);

        // 获得2力量
        await PowerCmd.Apply<StrengthPower>(base.Owner.Creature, 2m, base.Owner.Creature, null);
    }
}
