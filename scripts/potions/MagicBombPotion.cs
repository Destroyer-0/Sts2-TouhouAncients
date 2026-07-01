using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.PotionPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TouhouAncients.Scripts.potions;

/// <summary>
/// 轰炸魔废：变化你手中的所有牌。
/// </summary>
[RegisterPotion(typeof(SharedPotionPool))]
public sealed class MagicBombPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    public override PotionAssetProfile AssetProfile => new()
    {
        ImagePath = $"res://images/potion/{GetType().Name}.png",
        OutlinePath = $"res://images/potion/{GetType().Name}_outline.png"
    };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var cards = PileType.Hand.GetPile(base.Owner).Cards.ToList();
        var rng = base.Owner.RunState.Rng.CombatCardGeneration;

        foreach (var card in cards)
        {
            await CardCmd.TransformToRandom(card, rng);
        }
    }
}
