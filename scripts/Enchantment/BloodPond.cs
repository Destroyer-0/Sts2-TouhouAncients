using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 血池的有机物附魔：将卡牌的费用与辉星消耗降至0，获得重放1。
/// 通过附魔的持久化机制确保读档后修改不丢失。
/// </summary>
public class BloodPond : TouhouAncientEnchantmentModel
{
    public override bool HasExtraCardText => true;

    public override bool CanBeRandomSelected => false;

    protected override void OnEnchant()
    {
        // 费用降至0
        Card.EnergyCost.UpgradeBy(-10000);

        // 辉星消耗降至0
        if (Card.CurrentStarCost > 0)
        {
            Card.TryModifyStarCost(Card, 0, out _);
        }

        // 获得重放1
        Card.BaseReplayCount += 1;

        // 添加血池关键词（用于UI显示）
        Card.AddKeyword(TouhouAncientKeywords.TouhouAncientDropToBloodPond);
    }
}
