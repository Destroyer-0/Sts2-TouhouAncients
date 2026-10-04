using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 天赐甲胄：在每个回合开始时获得{Energy:energyIcons()}。每回合第一次从卡牌中获得的格挡值减半。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class HolyArmor : TouhouAncientRelics
{
    // 本回合触发减半的牌；非空即代表本回合已用过
    private CardModel? _halvedCard;
    private bool _halvedThisTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.ForEnergy(this),
            HoverTipFactory.Static(StaticHoverTip.Block),
        ];

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }

    public override Task BeforeCombatStart()
    {
        ResetHalving();
        return Task.CompletedTask;
    }

    // 纯查询 hook：预览（BlockVar）也会调用，禁止在这里 Flash
    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != base.Owner.Creature) return 1m;
        if (!props.IsCardOrMonsterMove()) return 1m;
        if (cardSource == null) return 1m;
        // 只对触发牌（尚未确定时任意牌）生效，其余牌本回合不再减半
        if (_halvedCard != null && _halvedCard != cardSource) return 1m;
        if (cardSource.Owner != base.Owner) return 1m;
        if (_halvedThisTurn) return 1m;
        return 0.5m;
    }

    // 预览不会走到这里（modifiers 被丢弃），只有真正结算加格挡才会
    public override Task AfterModifyingBlockAmount(decimal modifiedAmount, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (modifiedAmount <= 0m) return Task.CompletedTask;
        if (cardSource == null) return Task.CompletedTask;
        if (_halvedCard != null) return Task.CompletedTask; // 同一张牌多段格挡只闪一次
        Flash();
        _halvedCard = cardSource;
        base.Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return Task.CompletedTask;
        if (cardPlay.Card != _halvedCard) return Task.CompletedTask;
        if (_halvedThisTurn) return Task.CompletedTask;
        // 牌打完才算本回合用完，保证同牌多段格挡仍被减半
        _halvedThisTurn = true;
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(base.Owner.Creature)) return Task.CompletedTask;
        ResetHalving();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _halvedCard = null;
        _halvedThisTurn = false;
        base.Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    private void ResetHalving()
    {
        _halvedCard = null;
        _halvedThisTurn = false;
        base.Status = RelicStatus.Active;
    }
}
