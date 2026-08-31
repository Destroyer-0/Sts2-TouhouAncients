using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 付丧之力X：获得虚无。计数归零后，无论何处，将这张牌打出。
/// 回合结束时，如果这张牌在本回合没有被打出，计数-1。
/// </summary>
public class Tsukumogami : TouhouAncientEnchantmentModel
{
    public override bool ShowAmount => true;
    public override bool IsStackable => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Ethereal)];

    /// <summary>
    /// 本回合是否已经打出过这张牌。
    /// </summary>
    private bool playedThisTurn;

    protected override void OnEnchant()
    {
        base.Card.AddKeyword(CardKeyword.Ethereal);
    }

    public override Task BeforeCombatStart()
    {
        playedThisTurn = false;
        return base.BeforeCombatStart();
    }

    /// <summary>
    /// 打出时：记录本回合已打出，回合结束不再倒计时。
    /// </summary>
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay == null) return;
        if (cardPlay.Card != base.Card) return;
        playedThisTurn = true;
    }

    /// <summary>
    /// 回合结束时：如果这张牌在本回合没有被打出，计数-1。
    /// 计数归零后附魔失效，并无论何处自动打出。
    /// </summary>
    public override async Task AfterAutoPostPlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (!HasCard) return;
        if (player!= Card.Owner) return;
        if (base.Status != EnchantmentStatus.Normal) return;

        if (playedThisTurn)
        {
            // 本回合已打出：重置标记，计数不减
            playedThisTurn = false;
            return;
        }

        if (base.Amount <= 1)
        {
            // 计数归零：附魔失效，并无论何处自动打出
            base.Amount = 0;
            base.Status = EnchantmentStatus.Disabled;
            await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), base.Card, target: null);
        }
        else
        {
            base.Amount -= 1;
        }
    }
}
