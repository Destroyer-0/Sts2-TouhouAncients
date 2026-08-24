using MegaCrit.Sts2.Core.Entities.Powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 毒人偶：梅蒂欣·梅兰可莉的被动能力，Counter 层数表示毒人偶数量。
/// 铃铃的神经毒素按此层数向玩家弃牌堆加入毒素；铃铃倒下时层数减少 1（不会少于 1）。
/// 加牌与减层逻辑分别在 <see cref="TouhouAncients.Scripts.monsters.LingLingMonster"/> 中实现。
/// </summary>
public class PoisonDollPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
