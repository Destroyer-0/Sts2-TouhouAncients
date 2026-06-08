using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 弹幕地狱 Power（由纯粹的弹幕地狱施加）
/// - 攻击牌本回合免费打出
/// - 打出攻击牌时抽一张牌
/// - 手中没有攻击牌时结束回合
/// </summary>
public class HellOfBulletsPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>
    /// BeforeFlush 阶段：让所有攻击牌免费打出
    /// 并检测手中无攻击牌时结束回合
    /// </summary>
    public override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner.Player) return;
        if (base.Owner.CombatState == null) return;

        var hand = PileType.Hand.GetPile(player).Cards;

        // 所有攻击牌免费打出
        foreach (var card in hand)
        {
            if (card.Type == CardType.Attack)
            {
                card.EnergyCost.SetThisTurn(0);
            }
        }

        // 手中没有攻击牌时结束回合
        bool hasAttackCard = hand.Any(c => c.Type == CardType.Attack);
        if (!hasAttackCard)
        {
            // 移除自身后再结束回合，避免重复触发
            await PowerCmd.Remove(this);
            PlayerCmd.EndTurn(player, canBackOut: true);
        }
    }

    /// <summary>
    /// 打出攻击牌后抽一张牌
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner.Player) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        await CardPileCmd.Draw(choiceContext, 1, base.Owner.Player, fromHandDraw: true);
    }

    /// <summary>
    /// 回合结束时自动移除
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
