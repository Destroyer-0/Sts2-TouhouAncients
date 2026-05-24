using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class SkyHat : TouhouAncientRelics
{
    public override bool ShowCounter => DisplayAmount > -1;

    public override int DisplayAmount
    {
        get
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                return -1;
            }

            if (base.IsCanonical)
            {
                return -1;
            }

            return AccumulatedBlock;
        }
    }

    private int _accumulatedBlock = 0;

    public int AccumulatedBlock
    {
        get => _accumulatedBlock;
        set
        {
            AssertMutable();
            _accumulatedBlock = value;
            InvokeDisplayAmountChanged();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("BlockPerIntangible", 23m), new CardsVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Soul>(true);

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props,
        CardModel? cardSource)
    {
        if (creature != base.Owner.Creature) return;
        if (creature.CombatState == null) return;
        var gained = (int)amount;
        AccumulatedBlock += gained;
        var threshold = (int)base.DynamicVars["BlockPerIntangible"].BaseValue;
        while (AccumulatedBlock >= threshold)
        {
            AccumulatedBlock -= threshold;
            Flash();
            for (int i = 0; (decimal)i < base.DynamicVars.Cards.BaseValue; i++)
            {
                CardModel card = creature.CombatState.CreateCard(ModelDb.Card<Soul>(), base.Owner);
                CardCmd.Upgrade(card);
                CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw,
                    addedByPlayer: true, CardPilePosition.Random));
            }
        }
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        AccumulatedBlock = 0;
        return Task.CompletedTask;
    }
}