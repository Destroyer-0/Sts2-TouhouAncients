using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 不休的恚恨 — 受到敌人的伤害时，结算后为该敌人施加等同于伤害量的来源于你的怨仇。
/// 怨仇：对来源造成伤害降低 25%。来源打出攻击牌时，受到等同于怨仇层数的伤害并使层数 -1。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class CeaselessResentment : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /// <summary>
    /// 受到伤害后，为攻击者施加怨仇
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer == null) return;
        if (dealer == base.Owner.Creature) return; // 不是自己打自己
        if (result.TotalDamage <= 0) return;

        // 结算后，为攻击者施加怨仇（层数 = 伤害量）
        var existingPower = dealer.GetPower<YuanChouPower>();
        if (existingPower != null)
        {
            // 已有怨仇→增加层数
            existingPower.Source = base.Owner.Creature;
            await PowerCmd.ModifyAmount(existingPower, result.TotalDamage, null, null);
        }
        else
        {
            // 新建怨仇
            var power = (YuanChouPower)ModelDb.Power<YuanChouPower>().ToMutable();
            power.Source = base.Owner.Creature;
            await PowerCmd.Apply(power, dealer, result.TotalDamage, base.Owner.Creature, null);
        }
    }

    /// <summary>
    /// 来源（玩家）打出攻击牌时，所有有怨仇的敌人受到伤害
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        var enemies = base.Owner.Creature.CombatState.GetOpponentsOf(base.Owner.Creature)
            .Where(c => c.IsAlive);

        foreach (var enemy in enemies)
        {
            var yuanChou = enemy.GetPower<YuanChouPower>();
            if (yuanChou == null || yuanChou.Amount <= 0) continue;
            if (yuanChou.Source != base.Owner.Creature) continue;

            var amount = yuanChou.Amount;
            Flash();

            // 造成等同于怨仇层数的伤害，然后层数 -1
            await DamageCmd.Attack(amount)
                .FromCard(cardPlay.Card)
                .Targeting(enemy)
                .Execute(choiceContext);

            await PowerCmd.ModifyAmount(yuanChou, -1m, null, null);

            // 如果归零则移除
            if (yuanChou.Amount <= 0)
            {
                await PowerCmd.Remove(yuanChou);
            }
        }
    }
}
