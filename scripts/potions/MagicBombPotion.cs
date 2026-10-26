using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace TouhouAncients.Scripts.potions;

/// <summary>
/// 轰炸魔废：变化并升级你手中的所有牌。
/// </summary>
[Pool(typeof(SharedPotionPool))]
public sealed class MagicBombPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;
    public override string? CustomPackedImagePath => $"res://images/potion/{GetType().Name}.png";
    public override string? CustomPackedOutlinePath => $"res://images/potion/{GetType().Name}_outline.png";

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var cards = PileType.Hand.GetPile(base.Owner).Cards.ToList();
        var rng = base.Owner.RunState.Rng.CombatCardGeneration;

        // 变化得到的新牌是未升级的，升级必须排在变化之后
        List<CardModel> transformed = new();
        foreach (var card in cards)
        {
            var result = await CardCmd.TransformToRandom(card, rng);
            if (result.success)
            {
                transformed.Add(result.cardAdded);
            }
        }

        CardCmd.Upgrade(transformed, CardPreviewStyle.HorizontalLayout);
    }
}
