using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 铃铃神经毒素的意图：向玩家弃牌堆加入毒素的数量等于梅蒂欣当前的毒人偶层数。
/// 因毒人偶层数会变化（初始 1，"我可爱的铃铃" +1，铃铃倒下 -1），
/// 在显示意图时动态读取层数，保证头顶数字与实际加入数量一致。
/// 实际加牌逻辑在 <see cref="LingLingMonster.PoisonMove"/> 中实现。
/// </summary>
public sealed class LingLingPoisonIntent : StatusIntent
{
    public LingLingPoisonIntent() : base(1)
    {
    }

    /// <summary>
    /// 当前毒人偶层数：同队梅蒂欣身上的 <see cref="PoisonDollPower"/> 层数，取不到时回退为 1。
    /// </summary>
    private static int GetPoisonDollCount(Creature owner)
    {
        Creature? medicine = owner.CombatState?
            .GetTeammatesOf(owner)
            .FirstOrDefault(c => c is { Monster: MedicineMelancholyMonster, IsDead: false });
        if (medicine == null) return 1;
        return medicine.GetPower<PoisonDollPower>()?.Amount ?? 1;
    }

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        LocString label = IntentLabelFormat;
        label.Add("CardCount", GetPoisonDollCount(owner));
        return label;
    }

    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        LocString description = base.GetIntentDescription(targets, owner);
        description.Add("CardCount", GetPoisonDollCount(owner));
        return description;
    }
}
