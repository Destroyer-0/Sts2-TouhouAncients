using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.relics.IdleKoishi;

/// <summary>
/// 恋语耳麦：进入嗨到爆后抽 2 张牌，并让随机 2 张手牌本回合耗能变为 0。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class KoiHeadset : TouhouAncientRelics, IHypedEnteredListener
{
    private const int Cards = 2;
    private const int FreeCards = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Cards", Cards),
        new DynamicVar("FreeCards", FreeCards),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public async Task AfterHypedEntered(PlayerChoiceContext choiceContext)
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        Flash();
        await CardPileCmd.Draw(choiceContext, Cards, player);

        var hand = PileType.Hand.GetPile(player).Cards
            .Where(card => card.EnergyCost.GetWithModifiers(CostModifiers.All) > 0)
            .ToList();
        if (hand.Count == 0) return;

        var picks = hand.UnstableShuffle(player.RunState.Rng.CombatCardGeneration).Take(FreeCards);
        foreach (var card in picks)
        {
            card.EnergyCost.SetThisTurn(0);
        }
    }
}
