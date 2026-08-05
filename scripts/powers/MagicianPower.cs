using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 普通的魔法使：魔理沙的被动能力。
/// 逃逸速度会偷走玩家弃牌堆顶的牌存入 StolenCards，极限火花·蓄力时归还到手牌。
/// </summary>
public class MagicianPower : TouhouAncientPowerModel
{
    private List<CardModel> _stolenCards = new List<CardModel>();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>被偷走的牌（牌自带 Owner，归还时回到对应玩家手牌）。</summary>
    public List<CardModel> StolenCards
    {
        get => _stolenCards;
        set
        {
            AssertMutable();
            _stolenCards = value;
        }
    }
}
