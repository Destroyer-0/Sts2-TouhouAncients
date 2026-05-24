using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 生者必灭之理的计数 Power。
/// 4个回合后，清除所有敌人的人工制品并给予9999层灾厄。
/// 每次受到伤害，延长1回合触发时机并给予自身10灾厄。
/// </summary>
public class LifeMustPerishPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DoomAmount", 9999m),
        new DynamicVar("SelfDoomAmount", 10m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ArtifactPower>(),
        HoverTipFactory.FromPower<DoomPower>()
    ];

    private class Data
    {
        public int remainingTurns;
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override int DisplayAmount => RemainingTurns;

    public void SetStartingTurns(int turns) => RemainingTurns = turns;
    
    public int RemainingTurns
    {
        get => GetInternalData<Data>().remainingTurns;
        set
        {
            AssertMutable();
            GetInternalData<Data>().remainingTurns = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner) return;
        if (result.UnblockedDamage <= 0) return;
        if (props.HasFlag(ValueProp.Unblockable)) return;

        Flash();
        // 延长1回合（增加层数即延长）
        RemainingTurns++;
        // 给予自身灾厄
        await PowerCmd.Apply<DoomPower>(base.Owner, base.DynamicVars["SelfDoomAmount"].BaseValue, base.Owner, null);
    }

    /// <summary>
    /// 在玩家回合开始时减少计数
    /// </summary>
    public override async Task AfterTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Side) return;
        if (Owner.CombatState == null) return;
        if (base.Owner.IsDead) return;
        // 减少计数

        if (RemainingTurns <= 1)
        {
            Flash();
            // 触发：清除所有敌人的人工制品并给予9999层灾厄
            var enemies = Owner.CombatState.GetOpponentsOf(base.Owner).Where(e => e.IsAlive);
            foreach (var enemy in enemies)
            {
                // 清除人工制品
                if (enemy.HasPower<ArtifactPower>())
                {
                    await PowerCmd.Remove<ArtifactPower>(enemy);
                }

                // 给予灾厄
                await PowerCmd.Apply<DoomPower>(enemy, base.DynamicVars["DoomAmount"].BaseValue, base.Owner, null);
            }

            // 移除自身
            await PowerCmd.Remove(this);
        }
        else
        {
            Flash();
            RemainingTurns--;
        }
    }
}