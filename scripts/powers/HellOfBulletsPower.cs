using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
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
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner.Creature != Owner || card.Type is not CardType.Attack)
        {
            return false;
        }

        modifiedCost = default(decimal);
        return true;
    }

    /// <summary>
    /// 打出攻击牌后抽一张牌
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner.Player) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(base.Owner,
            VfxColor.Purple));
        await CardPileCmd.Draw(choiceContext, Amount, base.Owner.Player, fromHandDraw: true);
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Player == null)
        {
            return;
        }

        if (PileType.Hand.GetPile(Owner.Player).Cards.Count(x => x.Type == CardType.Attack) == 0)
        {
            PlayerCmd.EndTurn(Owner.Player, false);
        }
    }

    /// <summary>
    /// 回合结束时自动移除
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == base.Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
