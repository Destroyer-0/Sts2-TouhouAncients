using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 至贫形态 Power (Counter, 可叠加)
/// 打出耗能为0的牌或以0能量结束回合时，
/// 每1层给予随机一个敌人以下一个Debuff：
///   1 虚弱 / 1 易伤 / 3 中毒 / 8 灾厄
/// （每层可命中不同目标）
/// </summary>
public class PoorestFormPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DoomPower>()
    ];

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != base.Owner.Side) return;
        if (!base.Owner.IsPlayer || Owner.Player == null) return;
        if (!participants.Contains(Owner)) return;

        if (Owner.CombatState == null) return;
        if (Owner.Player.PlayerCombatState == null) return;

        IReadOnlyList<CardModel> cards = PileType.Hand.GetPile(Owner.Player).Cards;
        var remainingEnergy = cards.Where(c=>!c.EnergyCost.CostsX&& c.EnergyCost.GetResolved()>0).Sum(c=>c.EnergyCost.GetResolved());
        var doomPowerTime = remainingEnergy - Owner.Player.PlayerCombatState.Energy;

        for (int i = 0; i < doomPowerTime; i++)
        {
            Creature creature = base.Owner.Player.RunState.Rng.CombatTargets.NextItem(base.Owner.CombatState.HittableEnemies);
            await PowerCmd.Apply<DoomPower>(choiceContext, creature, Amount, Owner, null);
        }
    }
}