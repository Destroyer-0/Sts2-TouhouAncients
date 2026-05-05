using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class Zhihuijizhongbing : TouhouAncientRelics
{
    private bool _anyCardsPlayedThisTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new RepeatVar(3),
        new PowerVar<HotfixPower>("FocusPower", 3m),
        new DynamicVar("IceOrbNum", 2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FocusPower>(),
        HoverTipFactory.FromOrb<FrostOrb>(),
    ];

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Creature.Side && combatState.RoundNumber <= 1)
        {
            Flash();
            await OrbCmd.AddSlots(base.Owner, base.DynamicVars.Repeat.IntValue);
        }
        if (side == base.Owner.Creature.Side)
        {
            _anyCardsPlayedThisTurn = false;
        }
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (!CombatManager.Instance.IsInProgress) return Task.CompletedTask;
        if (_anyCardsPlayedThisTurn) return Task.CompletedTask;
        if (cardPlay.Card.Owner != base.Owner) return Task.CompletedTask;

        _anyCardsPlayedThisTurn = true;
        return Task.CompletedTask;
    }

    public override async Task BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player) return;
        if (_anyCardsPlayedThisTurn) return;

        // 没有打出任何牌 → 获得临时集中，生成冰霜充能球
        Flash();
        await PowerCmd.Apply<HotfixPower>(
            base.Owner.Creature,
            base.DynamicVars["FocusPower"].BaseValue,
            base.Owner.Creature,
            null);

        var iceCount = base.DynamicVars["IceOrbNum"].IntValue;
        for (int i = 0; i < iceCount; i++)
        {
            await OrbCmd.Channel<FrostOrb>(choiceContext, base.Owner);
        }
    }
}