using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 幸福的秘药：
/// 在Boss战开始时，用随机药水将你的空药水栏位填满，回复所有生命，
/// 并在本场战斗中升级你的所有卡牌、消耗你的所有诅咒牌。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HappinessElixir : TouhouAncientRelics
{
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room.RoomType != RoomType.Boss) return;
        if (base.Owner == null) return;

        var player = base.Owner;
        Flash();

        // 1. 用随机药水填满空药水栏位
        var rng = player.RunState.Rng.CombatPotionGeneration;
        for (var i = 0; i < player.MaxPotionCount; i++)
        {
            if (base.Owner.GetPotionAtSlotIndex(i) == null)
            {
                var randomPotion = PotionFactory.CreateRandomPotionOutOfCombat(player, rng).ToMutable();
                await PotionCmd.TryToProcure(randomPotion, player, i);
            }
        }

        // 2. 回复所有生命
        var maxHp = player.Creature.MaxHp;
        var healAmount = maxHp - player.Creature.CurrentHp;
        if (healAmount > 0)
        {
            await CreatureCmd.Heal(player.Creature, healAmount);
        }

        // 3. 升级牌组中的所有卡牌
        foreach (var card in player.Deck.Cards.ToList())
        {
            if (card.IsUpgradable && !card.IsUpgraded)
            {
                CardCmd.Upgrade(card);
            }
        }

        // 4. 从牌组移除所有诅咒牌
        var curses = player.Deck.Cards
            .Where(c => c.Type == CardType.Curse)
            .ToList();
        foreach (var curse in curses)
        {
            if (!curse.IsRemovable) continue;
            await CardPileCmd.RemoveFromDeck(curse, showPreview: false);
        }
    }
}