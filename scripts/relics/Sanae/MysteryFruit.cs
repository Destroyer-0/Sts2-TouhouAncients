using System;
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
/// 神秘果：每场战斗开始时获得 1 能量，并从抽牌堆随机将每种类型的牌各一张放入手牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MysteryFruit : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != base.Owner) return;
        if (base.Owner.PlayerCombatState == null || base.Owner.Creature.CombatState == null) return;
        if (base.Owner.PlayerCombatState.TurnNumber != 1) return;
        if (combatState != player.Creature.CombatState) return;

        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);

        int slots = CardPile.MaxCardsInHand - PileType.Hand.GetPile(player).Cards.Count;
        if (slots <= 0) return;

        IReadOnlyList<CardModel> drawPile = PileType.Draw.GetPile(player).Cards;
        List<CardModel> picks = [];
        foreach (CardType type in Enum.GetValues<CardType>())
        {
            if (picks.Count >= slots) break;
            if (type == CardType.None) continue;

            List<CardModel> candidates = drawPile.Where(c => c.Type == type).ToList();
            if (candidates.Count == 0) continue;

            picks.Add(candidates.UnstableShuffle(player.RunState.Rng.CombatCardSelection).First());
        }

        foreach (CardModel card in picks)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}
