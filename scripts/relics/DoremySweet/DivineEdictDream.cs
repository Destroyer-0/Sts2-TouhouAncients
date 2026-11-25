using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 神诏之梦：拾起时，失去 10 最大生命，再以不可跳过的奖励面板把一张随机形态牌加入你的牌组。
/// 「形态牌」= 稀有度为稀有、类型为能力、Entry 以 FORM 结尾的牌（先古形态因稀有度被排除）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DivineEdictDream : TouhouAncientRelics
{
    /// <summary>形态牌的 Entry 后缀。</summary>
    private const string FormEntrySuffix = "FORM";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HpLossVar(10m)];

    public override bool HasUponPickupEffect => true;

    /// <summary>形态牌判定：稀有 + 能力 + Entry 以 FORM 结尾。</summary>
    private static bool IsFormCard(CardModel card)
        => card.Type == CardType.Power
           && card.Rarity == CardRarity.Rare
           && card.Id.Entry.EndsWith(FormEntrySuffix, StringComparison.Ordinal);

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        Flash();
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            player.Creature,
            DynamicVars.HpLoss.BaseValue,
            cardSource: null);

        var template = RollFormCard(player);
        if (template == null) return;

        var rewards = new List<Reward>
        {
            // 拾起时就掷定牌面：SpecialCardReward 自带 SpecialCard 序列化，读档不会丢；
            // 面板上显示「将{牌名}加入你的牌组」，点一次即入组。
            new SpecialCardReward(player.RunState.CreateCard(template, player), player)
        };

        // 不可跳过：语义等同「拾起时即获得」，玩家必须把牌拿走
        await new RewardsSet(player).WithCustomRewards(rewards).WithSkippingDisallowed().Offer();
    }

    /// <summary>从全部卡池（各角色 + 共享池）随机取一张形态牌；按 Entry 排序，保证联机各端抽到同一张。</summary>
    private CardModel? RollFormCard(Player player)
    {
        // Uniform 不会掷稀有度，避免筛完没有对应稀有度而抛异常（照御阿礼·砚）
        var options = CardCreationOptions.ForNonCombatWithUniformOdds(ModelDb.AllCardPools, IsFormCard);

        var candidates = options.GetPossibleCards(player)
            .Distinct()
            .OrderBy(card => card.Id.Entry, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0) return null;
        return player.PlayerRng.Rewards.NextItem(candidates);
    }
}
