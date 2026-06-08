using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Orbs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 战栗的冻星 — 每打出3张攻击牌，生成一个冰霜充能球，并交替将一张带有虚无的战栗/主宰加入手牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class TremblingFrozenStar : TouhouAncientRelics
{
    private int _attackCount;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => _attackCount;

    private int AttackCount
    {
        get => _attackCount;
        set
        {
            AssertMutable();
            _attackCount = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("AttackThreshold", 3)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new []{ HoverTipFactory.FromOrb<FrostOrb>(),HoverTipFactory.FromKeyword(CardKeyword.Ethereal)}
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<Tremble>())
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<Dominate>());

    public override Task BeforeCombatStart()
    {
        AttackCount = 0;
        RefreshCounter();
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (base.Owner.Creature.CombatState == null) return;
        if (cardPlay.Card.Type != CardType.Attack) return;
        if (!cardPlay.IsFirstInSeries) return;

        AttackCount++;
        RefreshCounter();

        if (AttackCount == 3)
        {
            RefreshCounter();
            Flash();

            // 生成冰霜充能球
            await OrbCmd.Channel<FrostOrb>(context, base.Owner);

            // 交替加入战栗/主宰（带虚无）
            var tremble = base.Owner.Creature.CombatState.CreateCard<Tremble>(base.Owner);
            tremble.AddKeyword(CardKeyword.Ethereal);
            await CardPileCmd.AddGeneratedCardToCombat(tremble, PileType.Hand, addedByPlayer: true);
        }

        if (AttackCount == 6)
        {
            AttackCount = 0;
            RefreshCounter();
            Flash();
            await OrbCmd.Channel<FrostOrb>(context, base.Owner);
            var dominate = base.Owner.Creature.CombatState.CreateCard<Dominate>(base.Owner);
            dominate.AddKeyword(CardKeyword.Ethereal);
            await CardPileCmd.AddGeneratedCardToCombat(dominate, PileType.Hand, addedByPlayer: true);
        }

    }

    public override Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom _)
    {
        AttackCount = 0;
        base.Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    private void RefreshCounter()
    {
        InvokeDisplayAmountChanged();
    }
}
