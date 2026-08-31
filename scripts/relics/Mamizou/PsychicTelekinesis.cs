using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
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
/// 超能念力：每当你抽到能力牌时，额外抽{Cards}张牌。
/// 每回合首次打出能力牌时，将{Copies}张该牌的复制品加入弃牌堆。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class PsychicTelekinesis : TouhouAncientRelics
{
    private bool _powerPlayedThisTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Cards", 1m),
        new DynamicVar("Copies", 1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            _powerPlayedThisTurn = false;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 抽到能力牌时额外抽牌。
    /// </summary>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner != base.Owner) return;
        if (card.Type != CardType.Power) return;
        if (base.Owner.PlayerCombatState == null) return;

        Flash();
        var drawCount = base.DynamicVars["Cards"].IntValue;
        await CardPileCmd.Draw(choiceContext, drawCount, base.Owner, fromHandDraw: false);
    }

    /// <summary>
    /// 每回合首次打出能力牌时复制到弃牌堆。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (cardPlay.Card.Type != CardType.Power) return;
        if (_powerPlayedThisTurn) return;

        _powerPlayedThisTurn = true;

        Flash();

        var copyCount = base.DynamicVars["Copies"].IntValue;
        for (int i = 0; i < copyCount; i++)
        {
            CardModel card = cardPlay.Card.CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, base.Owner);
        }
    }
}
