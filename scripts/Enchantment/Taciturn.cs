using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 缄口：耗能减 1；打出此牌后本回合只能再打出 1 张牌。
/// </summary>
public class Taciturn : TouhouAncientEnchantmentModel
{
    public override bool HasExtraCardText => false;

    // 只由「无暇之梦」指定赋予，不进随机附魔池
    public override bool CanBeRandomSelected => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<EncorePower>()];

    protected override void OnEnchant()
    {
        if (!HasCard) return;
        // 要随牌进牌组长期生效，不能用 AddUntilPlayed；X 费牌 UpgradeBy 内部自动跳过
        Card.EnergyCost.UpgradeBy(-DynamicVars.Energy.IntValue);
    }

    public override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card) return Task.CompletedTask;
        if (base.Status != EnchantmentStatus.Normal) return Task.CompletedTask;

        // 用原版 EncorePower：只限制后续出牌，本牌已结算不受影响
        return PowerCmd.Apply<EncorePower>(choiceContext, Card.Owner.Creature, 1m, Card.Owner.Creature, Card);
    }
}
