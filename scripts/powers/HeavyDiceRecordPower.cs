using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 灌铅骰子用来“记录一张手牌”的战斗内标记 Power（无层数，Single / Instanced）。
/// 内部只保存被记录卡牌的战斗内克隆（记录的是克隆，不影响原牌）；
/// 下次玩家抽牌阶段结束后由遗物取出克隆兑现。
/// 该 Power 仅作为“有记录”的状态标记，且负责把所记录牌名显示在遗物/自身描述上。
/// </summary>
public class HeavyDiceRecordPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    // 在自身 tooltip 上展示所记录牌的名称
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("RecordedCard")];

    private class Data
    {
        public CardModel? recordedClone;
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    /// <summary>是否有正在生效的记录（有记录时本 Power 才会存在于玩家身上）。</summary>
    public bool HasRecord => GetInternalData<Data>().recordedClone != null;

    /// <summary>记录一张牌（战斗内克隆）。覆盖式：新的记录会替换旧的记录。</summary>
    public void SetRecordedCard(CardModel card)
    {
        Data data = GetInternalData<Data>();
        data.recordedClone = card.CreateClone();
        ((StringVar)base.DynamicVars["RecordedCard"]).StringValue = card.Title;
        InvokeDisplayAmountChanged();
    }

    /// <summary>取出所记录的克隆并清除记录（取出后 Power 不再持有任何卡）。</summary>
    public CardModel? TakeRecordedCard()
    {
        Data data = GetInternalData<Data>();
        CardModel? clone = data.recordedClone;
        data.recordedClone = null;
        ((StringVar)base.DynamicVars["RecordedCard"]).StringValue = "";
        InvokeDisplayAmountChanged();
        return clone;
    }

    /// <summary>仅清除记录（不取出）。</summary>
    public void ClearRecord()
    {
        GetInternalData<Data>().recordedClone = null;
        ((StringVar)base.DynamicVars["RecordedCard"]).StringValue = "";
        InvokeDisplayAmountChanged();
    }
}
