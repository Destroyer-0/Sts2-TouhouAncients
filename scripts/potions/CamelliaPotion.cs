using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TouhouAncients.Scripts.potions;

/// <summary>
/// 卡米莉亚：恢复你的所有生命，用随机药水将你的空药水栏位填满，
/// 如果处于战斗中，升级你的所有手牌。
/// </summary>
[RegisterPotion(typeof(EventPotionPool))]
public sealed class CamelliaPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Event;

    public override PotionUsage Usage => PotionUsage.AnyTime;

    public override TargetType TargetType => TargetType.AnyPlayer;

    public override PotionAssetProfile AssetProfile => new()
    {
        ImagePath = $"res://images/potion/{GetType().Name}.png",
        OutlinePath = $"res://images/potion/{GetType().Name}_outline.png"
    };


    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (target == null) return;
        var player = base.Owner.RunState.Players.First(x => x.Creature == target);

        // 1. 恢复所有生命
        await CreatureCmd.Heal(target, target.MaxHp);

        // 2. 用随机药水填满空药水栏位
        var rng = player.RunState.Rng.CombatPotionGeneration;
        while (player.HasOpenPotionSlots)
        {
            var randomPotion = PotionFactory.CreateRandomPotionOutOfCombat(player, rng).ToMutable();
            if (!(await PotionCmd.TryToProcure(randomPotion, player)).success)
            {
                break;
            }
        }
        
        // 3. 如果处于战斗中，升级你的全部卡牌（参照神化）
        if (MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress && player.PlayerCombatState != null)
        {
            foreach (var card in player.PlayerCombatState.AllCards)
            {
                if (card.IsUpgradable)
                {
                    CardCmd.Upgrade(card);
                }
            }
        }
    }
}