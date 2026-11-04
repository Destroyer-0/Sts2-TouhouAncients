using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 掌上的纯光：拾起时把初始打击/防御附魔纯粹；每回合开始按纯粹牌数对所有敌人造成等量伤害。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class PareLightOfThePalm : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<PureLight>(1);

    /// <summary>牌组里还有初始打击/防御才作为选项出现，否则本遗物毫无意义。</summary>
    public override bool CanAppear(Player? player)
    {
        if (player?.Creature == null) return false;

        return player.Deck.Cards.Any(IsBasicStrikeOrDefend);
    }

    public override Task AfterObtained()
    {
        var player = base.Owner;
        if (player == null) return Task.CompletedTask;

        foreach (var card in player.Deck.Cards.ToList())
        {
            if (!IsBasicStrikeOrDefend(card)) continue;

            if (card.Enchantment != null) CardCmd.ClearEnchantment(card);
            CardCmd.Enchant<PureLight>(card, 1m);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        var combatState = player.PlayerCombatState;
        if (combatState == null) return;

        // 检索范围同「完美打击」：抽牌堆 + 手牌 + 弃牌堆 + 消耗堆
        int count = combatState.AllCards.Count(c => c.Enchantment is PureLight);
        if (count <= 0) return;

        var creatureCombatState = player.Creature.CombatState;
        if (creatureCombatState == null) return;

        Flash();
        // 群体伤害必须走 DamageVar 重载（decimal 重载会被当作单体结算）
        var damage = new DamageVar((decimal)count, ValueProp.Unpowered);
        await CreatureCmd.Damage(choiceContext, creatureCombatState.HittableEnemies, damage, player.Creature);
    }

    private static bool IsBasicStrikeOrDefend(CardModel card)
    {
        return card.Rarity == CardRarity.Basic
               && (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend));
    }
}
