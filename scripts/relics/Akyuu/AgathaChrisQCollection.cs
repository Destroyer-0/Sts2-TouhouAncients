using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 阿加莎·克里斯Q侦探集：手稿与小说集合体强化版。
/// ? 房间内每房间挡一次生命值损伤；攻击牌打非攻击意图敌人加伤；回合结束按场上敌人数保留手牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class AgathaChrisQCollection : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Damage", 5m), new DynamicVar("RetainCard", 1m)];

    private int usedFloor = -1;

    /// <summary>消耗时所在楼层；同一地图点共用一层，天然实现「每房间一次」且读档不回满。</summary>
    [SavedProperty]
    public int TouhouAncients_UsedFloor
    {
        get => usedFloor;
        set
        {
            AssertMutable();
            usedFloor = value;
            RefreshStatus();
        }
    }

    // 建筑师胜利房必须能打死玩家，否则终局卡住；BaseRoom 兜事件里再叠子房间的情况
    private static bool InUnknownRoom(IRunState runState)
        => runState.CurrentMapPoint?.PointType == MapPointType.Unknown
           && runState.BaseRoom?.IsVictoryRoom != true;

    private bool IsArmed
    {
        get
        {
            if (!IsMutable || base.Owner == null) return false;
            var runState = base.Owner.RunState;
            return InUnknownRoom(runState) && runState.TotalFloor != usedFloor;
        }
    }

    public override Task AfterObtained()
    {
        RefreshStatus();
        return Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        // 原版没有「离开房间」钩子，只能进下一个房间时刷新
        RefreshStatus();
        return Task.CompletedTask;
    }

    // 照缓冲：排在 Late，让其它减伤先结算，已减到 0 时不白吃一次
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner.Creature || !IsArmed) return amount;
        return 0m;
    }

    public override Task AfterModifyingHpLostAfterOsty()
    {
        // 只有真的改了数值才会被收进 modifiers，走到这里必定是这次挡下的
        TouhouAncients_UsedFloor = base.Owner.RunState.TotalFloor;
        Flash();
        return Task.CompletedTask;
    }

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner || player.Creature.CombatState is not { } combatState) return;
        if (!Hook.ShouldFlush(combatState, player)) return;

        // 场上每个活者敌人都算一次（不看意图）
        var enemyCount = combatState
            .GetCreaturesOnSide(CombatSide.Enemy)
            .Count(c => c.IsAlive && c.IsEnemy);
        if (enemyCount <= 0) return;

        var prefLoc = base.SelectionScreenPrompt;
        prefLoc.Add("Amount", enemyCount);
        List<CardModel> list = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(prefLoc, 0, enemyCount),
            context: choiceContext,
            player: player,
            filter: RetainFilter,
            source: this)).ToList();
        if (list.Count == 0) return;

        foreach (CardModel card in list)
        {
            card.GiveSingleTurnRetain();
        }
    }

    private bool RetainFilter(CardModel card)
    {
        return !card.ShouldRetainThisTurn;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource, CardPlay? cardPlay)
    {
        if (cardSource == null) return 0m;
        if (target == null || !target.IsAlive || !target.IsEnemy) return 0m;
        if (!props.IsPoweredAttack()) return 0m;
        if (!TouhouAncientCmd.IsPlayerDamageIncludePet(Owner, dealer)) return 0m;

        // 意图不是攻击的敌人才吃加成
        var isAttacking = target.Monster?.NextMove?.Intents.Any(i => i.IntentType == IntentType.Attack) == true;
        return isAttacking ? 0m : base.DynamicVars["Damage"].IntValue;
    }

    /// <summary>Active = 本房间可用；Disabled = 本房间已用掉；Normal = 不在 ? 房间。</summary>
    private void RefreshStatus()
    {
        if (!IsMutable) return;
        Player? owner = base.Owner;
        if (owner == null) return;

        var runState = owner.RunState;
        base.Status = !InUnknownRoom(runState) ? RelicStatus.Normal
            : runState.TotalFloor == usedFloor ? RelicStatus.Disabled
            : RelicStatus.Active;
        InvokeDisplayAmountChanged();
    }
}
