using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 降伏：打出后，将一张梦想封印置入手牌。
/// </summary>
public class Exorcism : CustomEnchantmentModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<DreamSeal>(upgrade:HasCard && Card.IsUpgraded)];

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay.Card != base.Card) return;
        if (base.Status != EnchantmentStatus.Normal) return;

        var player = base.Card?.Owner;
        if (player == null) return;

        // 创建梦想封印加入手牌
        var dreamSeal = player.RunState.CreateCard(ModelDb.Card<DreamSeal>(), player);
        if (Card.IsUpgraded)
        {
            CardCmd.Upgrade(dreamSeal);
        }
        await CardPileCmd.AddGeneratedCardsToCombat([dreamSeal], PileType.Hand, true);
    }
}
