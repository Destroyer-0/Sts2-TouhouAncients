using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 梦想封印·寂：1 费状态卡牌，可无限升级。
/// 打出获得 1 层虚弱（每次升级额外获得 1 层）。
/// 如果这张牌在你的手中，你不能打出攻击牌。
/// </summary>
[Pool(typeof(StatusCardPool))]
public class DreamSealSabi : ReimuBossDreamSealStatus
{
    public DreamSealSabi() : base(true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WeakPower>(choiceContext, base.Owner.Creature,
            base.DynamicVars["Amount"].BaseValue, base.Owner.Creature, this);
    }

    /// <summary>
    /// 手牌限制：当此牌在手中时，玩家不能打出攻击牌。
    /// 通过 ShouldPlay Hook 实现（CanPlay 内部调用 Hook.ShouldPlay，同时影响手动与自动打出）。
    /// 此牌自身是 Status 类型，天然不受自身限制；自动打出（AutoPlay）不受限制（与 Enthralled 一致）。
    /// </summary>
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner != base.Owner) return true;
        CardPile? pile = base.Pile;
        if (pile == null || pile.Type != PileType.Hand) return true;
        if (card == this) return true;
        if (autoPlayType != AutoPlayType.None) return true;
        return card.Type != CardType.Attack;
    }
}
