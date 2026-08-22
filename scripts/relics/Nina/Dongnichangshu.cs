using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 亘古不变的数字：每回合你的格挡首次大于 15 时，将格挡降低至 15，获得 1 能量并抽 2 张牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class Dongnichangshu : TouhouAncientRelics
{
    private bool _triggeredThisTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BlockLimit", 15m),
        new EnergyVar(1),
        new CardsVar(2),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(this),
        HoverTipFactory.Static(StaticHoverTip.Block),
    ];

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
        {
            return;
        }
        if (side == base.Owner.Creature.Side)
        {
            _triggeredThisTurn = false;
            Status = RelicStatus.Active;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != base.Owner.Creature) return;
        if (_triggeredThisTurn) return;

        var limit = base.DynamicVars["BlockLimit"].IntValue;
        if (creature.Block <= limit) return;

        // 每回合首次格挡大于 15：将格挡降低至 15，获得 1 能量并抽 2 张牌
        _triggeredThisTurn = true;
        Flash();

        await CreatureCmd.LoseBlock(new ThrowingPlayerChoiceContext(), creature, creature.Block - limit, creature);
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, base.Owner);
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), base.DynamicVars.Cards.BaseValue, base.Owner, fromHandDraw: false);
        
        Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }
}
