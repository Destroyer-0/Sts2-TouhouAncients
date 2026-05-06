using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

public class Miracle : EnchantmentModel
{
    public override bool CanEnchantCardType(CardType cardType)
    {
        // Only Attack, Skill, Power
        if ((uint)(cardType - 1) <= 2u)
        {
            return true;
        }
        return false;
    }

    protected override void OnEnchant()
    {
        base.Card.AddKeyword(CardKeyword.Exhaust);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        var player = base.Card.Owner;
        if (player == null) return;

        // 获得1能量
        await PlayerCmd.GainEnergy(1m, player);

        // 从所有玩家职业牌与无色牌中选一张随机的（不包括状态牌、诅咒牌）
        var eligibleCards = ModelDb.AllCards
            .Where(c => c.Type == CardType.Attack
                     || c.Type == CardType.Skill
                     || c.Type == CardType.Power)
            .ToList();

        if (eligibleCards.Count == 0) return;

        var randomCard = eligibleCards.UnstableShuffle(player.RunState.Rng.Niche).FirstOrDefault();
        if (randomCard == null) return;

        // 创建卡牌实例，附魔奇迹，加入抽牌堆
        var createdCard = player.RunState.CreateCard(randomCard, player);
        CardCmd.Enchant<Miracle>(createdCard, 1m);
        await CardPileCmd.Add(createdCard, PileType.Draw);
    }
}
