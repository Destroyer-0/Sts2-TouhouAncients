using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 至高之梦：每当你进入新的幕时，获得1瓶药水、50金币，最大生命提升5，
/// 获得1张牌，并随机升级1张牌。
///
/// 实现说明：
///   - 「获得1张牌」从当前角色的已解锁卡池中随机抽取（排除基础牌与诅咒牌），直接加入牌组。
///   - 「随机升级1张牌」从牌组中尚未升级且可升级的牌里随机抽取。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SupremeDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Potions", 1),
        new DynamicVar("Gold", 50),
        new DynamicVar("MaxHpGain", 5),
        new CardsVar(1),
        new DynamicVar("Upgrades", 1)
    ];

    public override async Task AfterActEntered()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        Flash();

        // 1) 获得药水
        var potionCount = base.DynamicVars["Potions"].IntValue;
        for (int i = 0; i < potionCount; i++)
        {
            var potion = PotionFactory.CreateRandomPotionOutOfCombat(
                player, player.RunState.Rng.CombatPotionGeneration);
            await PotionCmd.TryToProcure(potion.ToMutable(), player);
        }

        // 2) 获得金币
        var gold = base.DynamicVars["Gold"].IntValue;
        if (gold > 0)
        {
            await PlayerCmd.GainGold(gold, player);
        }

        // 3) 提升最大生命
        var maxHpGain = base.DynamicVars["MaxHpGain"].BaseValue;
        if (maxHpGain > 0)
        {
            await CreatureCmd.GainMaxHp(player.Creature, maxHpGain);
        }

        // 4) 获得1张牌
        var cardCount = base.DynamicVars.Cards.IntValue;
        if (cardCount > 0)
        {
            var candidates = player.Character.CardPool
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .Where(c => c.Rarity is not (CardRarity.Basic or CardRarity.Curse))
                .ToList();

            var results = new List<CardPileAddResult>();
            for (int i = 0; i < cardCount && candidates.Count > 0; i++)
            {
                var template = candidates[player.PlayerRng.Rewards.NextInt(0, candidates.Count)];
                var card = player.RunState.CreateCard(template, player);
                results.Add(await CardPileCmd.Add(card, PileType.Deck));
            }

            if (results.Count > 0)
            {
                CardCmd.PreviewCardPileAdd(results, 2f);
            }
        }

        // 5) 随机升级1张牌
        var upgradeCount = base.DynamicVars["Upgrades"].IntValue;
        for (int i = 0; i < upgradeCount; i++)
        {
            var upgradable = player.Deck.Cards
                .Where(c => c.IsUpgradable && !c.IsUpgraded)
                .ToList();

            if (upgradable.Count == 0) break;

            var target = upgradable[player.PlayerRng.Rewards.NextInt(0, upgradable.Count)];
            CardCmd.Upgrade(target);
            CardCmd.Preview(target);
        }
    }
}
