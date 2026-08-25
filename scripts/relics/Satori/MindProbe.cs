using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 心灵探测仪：若敌人造成的伤害与你的格挡值相差绝对值小于等于2，使其下回合眩晕。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MindProbe : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Threshold", 2m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [StunIntent.GetStaticHoverTip()];

    private List<Creature> _stunedEnemyCreature = new();

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _stunedEnemyCreature = new List<Creature>();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _stunedEnemyCreature.Clear();
        return base.AfterCombatEnd(room);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (dealer == null) return;
        if (dealer == base.Owner.Creature) return; // 不是自己打自己
        if (!result.WasFullyBlocked) return;
        if (!dealer.IsEnemy || _stunedEnemyCreature.Contains(dealer)) return;
        
        Flash();
        await StunEnemyWithNextIntent(dealer);
        _stunedEnemyCreature.Add(dealer);
    }

    /// <summary>
    /// 眩晕敌人，并指定其眩晕结束后应执行的下一意图（避免意图停留在当前意图上）。
    /// 参照盛碗虫（石）与盗宝袋兽的 FlutterPower 实现。
    /// </summary>
    private async Task StunEnemyWithNextIntent(Creature enemy)
    {
        var monster = enemy.Monster;
        if (monster == null) return;
        if (monster.MoveStateMachine == null) return;

        // StateLog 最后一项是敌人当前意图对应的状态，计算出它之后应执行的下一意图
        var nextState = monster.MoveStateMachine.StateLog.Last()
            .GetNextState(enemy, monster.RunRng.MonsterAi);

        // 传入下一意图 ID，眩晕回合结束后直接进入该意图，而不是停留在当前意图
        await CreatureCmd.Stun(enemy, nextState);
    }

    // public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount,
    //     ValueProp props, Creature? dealer, CardModel? cardSource)
    // {
    //     if (target != base.Owner.Creature) return;
    //     if (dealer == null) return;
    //     if (!dealer.IsEnemy) return; // 只对敌人有效
    //
    //     if (amount <= 0) return;
    //
    //     var block = target.Block;
    //     var diff = Math.Abs(amount - block);
    //
    //     if (diff <= 2m)
    //     {
    //         Flash();
    //         await CreatureCmd.Stun(dealer);
    //     }
    // }
}