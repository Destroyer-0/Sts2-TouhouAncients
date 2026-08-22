using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 狸之烟管：在你的回合结束时，变化任意张手牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class TunakiSmokingPipe : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Transform)];

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != base.Owner.Creature.Side) return;
        if (!participants.Contains(Owner.Creature)) return;
        if (base.Owner.PlayerCombatState == null) return;

        var hand = PileType.Hand.GetPile(base.Owner);
        if (hand.Cards.Count == 0) return;

        // 从手牌中选择任意张可变化的牌
        var selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 0, int.MaxValue),
            context: choiceContext,
            player: base.Owner,
            filter: card => card.IsTransformable,
            source: this)).ToList();

        if (selected.Count == 0) return;

        Flash();

        var rng = base.Owner.RunState.Rng.CombatCardGeneration;
        foreach (var card in selected)
        {
            await CardCmd.TransformToRandom(card, rng);
        }
    }
}
