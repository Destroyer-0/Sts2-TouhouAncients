using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 沉锚幽灵：回合开始时获得1能量。每场战斗开始时，将随机9张牌置入弃牌堆。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SunkenAnchorGhost : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(9)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + 1m;
    }

    /// <summary>
    /// 战斗开始后，从抽牌堆随机选择9张牌置入弃牌堆。
    /// </summary>
    public override async Task BeforeCombatStartLate()
    {
        if (base.Owner?.Creature?.CombatState == null) return;

        var player = base.Owner;
        var drawPile = player.PlayerCombatState.DrawPile;

        if (drawPile.IsEmpty) return;

        var cardsToDiscard = drawPile.Cards.ToList()
            .UnstableShuffle(player.RunState.Rng.CombatCardSelection)
            .Take(DynamicVars.Cards.IntValue)
            .ToList();

        if (cardsToDiscard.Count == 0) return;

        Flash();
        await CardPileCmd.Add(cardsToDiscard, PileType.Discard, CardPilePosition.Random);
    }
}