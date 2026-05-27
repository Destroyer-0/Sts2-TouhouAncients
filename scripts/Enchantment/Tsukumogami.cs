using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 付丧之力：获得虚无。
/// 抽牌阶段结束后，如果手牌中没有带付丧之力的牌，
/// 将随机一张带付丧之力的牌从抽牌堆/弃牌堆移动到手牌中。
/// </summary>
public class Tsukumogami : CustomEnchantmentModel
{
    public override bool CanEnchantCardType(CardType cardType)
    {
        // 只能附魔攻击、技能、能力
        return (uint)(cardType - 1) <= 2u;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Ethereal)];

    protected override void OnEnchant()
    {
        base.Card.AddKeyword(CardKeyword.Ethereal);
    }

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (!HasCard) return;
        if (player != Card.Owner) return;
        if (player.Creature.CombatState==null) return;
        if (PileType.Hand.GetPile(player).Cards.Count(x => x.Enchantment is Tsukumogami) == 0)
        {
            var allTsukumogami = player.Piles
                .Where(x => x.Type is PileType.Draw or PileType.Exhaust or PileType.Discard).SelectMany(x => x.Cards)
                .Where(x => x is { Enchantment: Tsukumogami, HasBeenRemovedFromState: false }).ToList();
            if (allTsukumogami.Count > 0)
            {
                var backCard = allTsukumogami
                    .TakeRandom(1, player.Creature.CombatState.RunState.Rng.CombatCardSelection).First();
                await CardPileCmd.Add(backCard, PileType.Hand);
            }
        }
    }

}
