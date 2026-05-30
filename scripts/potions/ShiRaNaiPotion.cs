using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.PotionPools;

namespace TouhouAncients.Scripts.potions;

/// <summary>
/// 希腊奶：从消耗堆选取一张牌放入手牌。
/// </summary>
[Pool(typeof(SharedPotionPool))]
public sealed class ShiRaNaiPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;
    public override string? CustomPackedImagePath => $"res://images/potion/{GetType().Name}.png";
    public override string? CustomPackedOutlinePath => $"res://images/potion/{GetType().Name}_outline.png";

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var card = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            (from c in PileType.Exhaust.GetPile(base.Owner).Cards
                orderby c.Rarity, c.Id
                select c).ToList(),
            base.Owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 0, 1)
        )).FirstOrDefault();

        if (card != null)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}
