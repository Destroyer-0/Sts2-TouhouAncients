using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;
using TouhouAncients.Scripts.Rewards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 独眼唐伞：本回合对意图攻击的敌人累计造成的攻击伤害超过其意图伤害时，令其本回合失去{StrengthDown}力量。
/// 斩杀负力量的敌人时，战斗奖励额外增加一次升级牌奖励。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class OneEyedKarakasa : TouhouAncientRelics
{
    private readonly Dictionary<Creature, int> _damageDealtThisTurn = new();
    private readonly HashSet<Creature> _strengthLostThisTurn = new();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("StrengthDown", 3m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (!TouhouAncientCmd.IsPlayerDamageIncludePet(base.Owner, dealer)) return;
        if (!props.IsPoweredAttack()) return;
        if (!target.IsEnemy || !target.IsAlive || result.TotalDamage <= 0) return;
        if (_strengthLostThisTurn.Contains(target)) return;
        if (target.Monster?.IntendsToAttack != true) return;

        int dealt = _damageDealtThisTurn.GetValueOrDefault(target) + result.TotalDamage;
        _damageDealtThisTurn[target] = dealt;
        if (dealt <= GetIntentDamage(target)) return;

        // 同回合内多次命中只结算一次，避免段数把力量一次抽干
        _strengthLostThisTurn.Add(target);
        Flash();
        await PowerCmd.Apply<OneEyedKarakasaStrengthDownPower>(choiceContext, target,
            base.DynamicVars["StrengthDown"].BaseValue, base.Owner.Creature, null);
    }

    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || !creature.IsEnemy) return Task.CompletedTask;
        if (!creature.Powers.All(power => power.ShouldOwnerDeathTriggerFatal())) return Task.CompletedTask;
        if (creature.GetPowerAmount<StrengthPower>() >= 0) return Task.CompletedTask;

        Flash();
        UpgradeCardReward.AddAsExtraReward(base.Owner);
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return Task.CompletedTask;
        ResetTurnTracking();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ResetTurnTracking();
        return Task.CompletedTask;
    }

    private void ResetTurnTracking()
    {
        _damageDealtThisTurn.Clear();
        _strengthLostThisTurn.Clear();
    }

    /// <summary>意图总伤害：多段攻击为单段伤害×段数，且已计入当前力量修正。</summary>
    private static int GetIntentDamage(Creature enemy)
    {
        if (enemy.Monster == null || enemy.CombatState == null) return 0;

        var targets = enemy.CombatState.PlayerCreatures;
        return enemy.Monster.NextMove.Intents.OfType<AttackIntent>()
            .Sum(intent => intent.GetTotalDamage(targets, enemy));
    }
}
