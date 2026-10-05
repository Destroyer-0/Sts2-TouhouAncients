using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using TouhouAncients.Scripts.Rewards;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 梦境三牌（幻梦无垠 / 深梦无觉 / 噩梦无终）共用：休息时追加一条「梦醒时」变化奖励，
/// 并从当前角色牌池按类型抽随机牌入堆。
/// </summary>
public abstract class DreamCycleCard : TouhouAncientCards
{
    protected DreamCycleCard(int energyCost, CardType type, CardRarity rarity, TargetType targetType,
        bool shouldShowInCardLibrary) : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    /// <summary>选择「休息」时，为这张牌加一条变化奖励。</summary>
    public override bool TryModifyRestSiteHealRewards(Player player, List<Reward> rewards, bool isMimicked)
    {
        // 事件假冒的休息不算
        if (player != base.Owner || isMimicked) return false;

        rewards.Add(new DreamAwakeningReward(player, this));
        return true;
    }

    /// <summary>衍生牌「打出前」费用减少量；幻梦无垠为 1，其余为 0。</summary>
    protected virtual int GeneratedCardCostReduction => 0;

    /// <summary>从当前角色牌池抽 <paramref name="count"/> 张指定类型的随机牌（去重）入堆，附加虚无。</summary>
    protected async Task AddRandomDreamCards(PileType pile, int count, params CardType[] types)
    {
        var player = base.Owner;
        if (player.Creature.CombatState == null) return;

        var pool = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => types.Contains(c.Type))
            .ToList();
        var cards = CardFactory
            .GetDistinctForCombat(player, pool, count, player.RunState.Rng.CombatCardGeneration)
            .ToList();

        var results = await CardPileCmd.AddGeneratedCardsToCombat(cards, pile, creator: player,
            CardPilePosition.Random);
        foreach (var result in results)
        {
            result.cardAdded.AddKeyword(CardKeyword.Ethereal);
            // 费用-1 保留到首次打出
            if (GeneratedCardCostReduction > 0)
                result.cardAdded.EnergyCost.AddUntilPlayed(-GeneratedCardCostReduction);
        }

        CardCmd.PreviewCardPileAdd(results);
    }
}
