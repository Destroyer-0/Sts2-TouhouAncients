using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 侦探小说集：你的攻击牌对意图不是攻击的敌人额外造成3伤害。
/// 回合结束时，场上每有一个意图是攻击的敌人，额外保留1张牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DetectiveStory : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Damage", 3m), new DynamicVar("RetainCard", 1m)];

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner || !Hook.ShouldFlush(player.Creature.CombatState, player))
        {
            return;
        }

        var attackingCount = base.Owner.Creature.CombatState
            .GetCreaturesOnSide(CombatSide.Enemy)
            .Count(c => c.IsAlive && c.IsEnemy && c.Monster?.NextMove?.Intents.Any(i => i.IntentType == IntentType.Attack) == true);
        if (attackingCount <= 0)
        {
            return;
        }
        var prefLoc = base.SelectionScreenPrompt;
        prefLoc.Add("Amount",attackingCount);
        List<CardModel> list = (await CardSelectCmd.FromHand(prefs: new CardSelectorPrefs(prefLoc, 0, attackingCount), context: choiceContext, player: base.Owner, filter: RetainFilter, source: this)).ToList();
        if (list.Count == 0)
        {
            return;
        }
        foreach (CardModel item in list)
        {
            item.GiveSingleTurnRetain();
        }
    }

    private bool RetainFilter(CardModel card)
    {
        return !card.ShouldRetainThisTurn;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (cardSource == null) return 0m;
        if (target == null || !target.IsAlive || !target.IsEnemy) return 0m;
        if (!props.IsPoweredAttack()) return 0m;
        if (dealer != base.Owner?.Creature) return 0m;

        // 检查敌人意图是否为攻击
        var isAttacking = target.Monster?.NextMove?.Intents.Any(i => i.IntentType == IntentType.Attack) == true;
        if (!isAttacking)
        {
            return base.DynamicVars["Damage"].IntValue;
        }

        return 0m;
    }
}
