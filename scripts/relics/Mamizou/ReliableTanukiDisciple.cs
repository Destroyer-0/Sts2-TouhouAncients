using System.Collections.Generic;
using System.Linq;
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
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 可靠的弟子狸：在你的回合开始时，随机将{Cards}张仆从俯冲、仆从打击、仆从捐躯加入你的手牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class ReliableTanukiDisciple : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<MinionDiveBomb>(),
        HoverTipFactory.FromCard<MinionStrike>(),
        HoverTipFactory.FromCard<MinionSacrifice>(),
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (player.Creature.CombatState == null) return;

        Flash();

        var count = base.DynamicVars["Cards"].IntValue;
        var minionCards = new CardModel[]
        {
            ModelDb.Card<MinionDiveBomb>(),
            ModelDb.Card<MinionStrike>(),
            ModelDb.Card<MinionSacrifice>(),
        };

        for (int i = 0; i < count; i++)
        {
            var canonical = minionCards[base.Owner.RunState.Rng.CombatCardGeneration.NextInt(minionCards.Length)];
            var card = player.Creature.CombatState.CreateCard(canonical, player);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, creator: base.Owner);
        }
    }
}
