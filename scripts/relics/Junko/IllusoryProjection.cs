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
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 迷幻的投影 — 在每场战斗开始时，获得混乱效果。
/// 2费用的卡牌打出2次，大于等于3费用的卡牌打出3次。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class IllusoryProjection : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CostTwo", 2),
        new DynamicVar("CostThree", 3)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<ConfusedPower>();
    
    public override async Task BeforeCombatStart()
    {
        // 获得混乱效果（参照 SneckoEye）
        await PowerCmd.Apply<ConfusedPower>(new ThrowingPlayerChoiceContext(), base.Owner.Creature, 1m, base.Owner.Creature, null);
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner != base.Owner) return playCount;

        if (card.EnergyCost.CostsX) return playCount;
        
        // 获取解析后的费用（混乱随机化后的最终费用）
        var cost = card.EnergyCost.GetResolved();

        if (cost == 2)
        {
            Flash();
            return playCount + 1; // 打出2次
        }

        if (cost >= 3)
        {
            Flash();
            return playCount + 2; // 打出3次
        }

        return playCount;
    }
}
