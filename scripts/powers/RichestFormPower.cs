using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 极奢形态 Power (Single)
/// - 打出非X费牌不再消耗能量，改为 1:10 消耗启动资金
/// - 启动资金不足时改为消耗金币
/// - 通过 TryModifyEnergyCostInCombat 将所有手牌费用置 0
/// - 在 AfterCardPlayed 中根据原耗能扣除资金/金币
/// </summary>
public class RichestFormPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CapitalPower>()
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        // 注册金币变化监听（获得或失去金币时刷新费用显示）
        var player = base.Owner.Player;
        if (player != null)
        {
            player.GoldChanged += OnGoldChanged;
        }
        return Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (card.Owner?.Creature != base.Owner) return false;
        if (card.EnergyCost.CostsX) return false;

        // 只修改手牌和打出区的牌
        if (card.Pile?.Type != PileType.Hand && card.Pile?.Type != PileType.Play) return false;

        // 耗能为 0 或负数的牌不需要修改
        if (originalCost <= 0) return false;

        // 检查能否支付原耗能 * 10 的费用（启动资金 + 金币）
        var requiredCost = originalCost * 10m;
        if (!CanAfford(requiredCost)) return false;

        modifiedCost = 0m;
        return true;
    }

    /// <summary>
    /// 检查当前玩家是否有足够的资金（启动资金 + 金币）支付指定费用
    /// </summary>
    private bool CanAfford(decimal requiredCost)
    {
        var player = base.Owner.Player;
        if (player == null) return false;

        // 启动资金
        var capital = base.Owner.GetPower<CapitalPower>();
        var availableCapital = capital?.Amount ?? 0m;

        // 玩家金币
        var availableGold = player.Gold;

        return (availableCapital + availableGold) >= requiredCost;
    }

    /// <summary>
    /// 金币变化后重新检测 CanAfford（获得或失去金币时）
    /// </summary>
    private void OnGoldChanged()
    {
        if (!CombatManager.Instance.IsInProgress) return;
        RefreshHandCardCosts();
    }

    /// <summary>
    /// 卡牌打出后，根据原耗能*10扣除资金/金币
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != base.Owner) return;
        if (cardPlay.Card.EnergyCost.CostsX) return;
        if (cardPlay.IsAutoPlay) return;
        if (!cardPlay.IsLastInSeries) return;

        var canonicalCost = cardPlay.Card.EnergyCost.Canonical;
        if (canonicalCost <= 0m) return;

        var costInCapital = canonicalCost * 10m;

        // 检查是否有启动资金
        var capital = base.Owner.GetPower<CapitalPower>();
        if (capital != null && capital.Amount > 0)
        {
            var deductFromCapital = System.Math.Min(capital.Amount, costInCapital);
            await PowerCmd.ModifyAmount(context, capital, -deductFromCapital, null, null);

            var remainder = costInCapital - deductFromCapital;
            if (remainder > 0m)
            {
                // 不足部分消耗金币
                await PlayerCmd.LoseGold(remainder, base.Owner.Player, GoldLossType.Spent);
            }
        }
        else
        {
            // 没有启动资金，直接消耗金币
            await PlayerCmd.LoseGold(costInCapital, base.Owner.Player, GoldLossType.Spent);
        }

        // 刷新手牌费用显示（资金变化后重新检测 CanAfford）
        RefreshHandCardCosts();
    }

    /// <summary>
    /// 刷新手牌中所有牌的费用显示，使 TryModifyEnergyCostInCombat 重新评估
    /// </summary>
    private void RefreshHandCardCosts()
    {
        var player = base.Owner.Player;
        if (player?.Creature?.CombatState == null) return;

        var hand = PileType.Hand.GetPile(player).Cards;
        foreach (var card in hand)
        {
            NCard.FindOnTable(card)?.PlayRandomizeCostAnim();
        }
    }
}
