using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 破厄真气：拾起时获得9张随机诅咒。可以打出诅咒牌，打出诅咒时获得1力量、1能量并抽2张牌。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class CurseBreakerQi : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        // 获得9张随机诅咒
        var cursePool = ModelDb.CardPool<CurseCardPool>();
        var allCurses = cursePool.AllCards.ToList();
        var rng = player.RunState.Rng.Shuffle;
        var cursesToAdd = allCurses.UnstableShuffle(rng).Take(9);
        await CardPileCmd.AddCursesToDeck(cursesToAdd, player);
    }

    // 允许打出诅咒牌
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner != base.Owner) return true;
        if (card is { Type: CardType.Curse })
        {
            return true;
        }
        return base.ShouldPlay(card, autoPlayType);
    }

    // 打出诅咒时获得奖励
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (cardPlay.Card.Type != CardType.Curse) return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
        await PlayerCmd.GainEnergy(1, base.Owner);
        await CardPileCmd.Draw(context, 2, base.Owner);
    }
}
