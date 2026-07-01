using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 无疚之面：拾起时，选择你牌组中的一张牌，将你牌组中所有同类型牌变化为其。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class GuiltlessFace : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        // 从牌组中选择一张牌
        var prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 1, 1);
        var selected = (await CardSelectCmd.FromDeckGeneric(player, prefs)).ToList();
        if (selected.Count == 0) return;

        var chosen = selected[0];
        var targetType = chosen.Type;

        // 找到牌组中所有同类型的其他牌
        var sameTypeCards = player.Deck.Cards
            .Where(c => c != chosen && c.Type == targetType && c.IsTransformable)
            .ToList();

        if (sameTypeCards.Count == 0) return;

        Flash();

        // 将所有同类型牌变换为选中的牌
        List<CardTransformation> transformations = new();

        foreach (var original in sameTypeCards)
        {
            var cardModel = base.Owner.RunState.CloneCard(chosen);
            if (original.Enchantment != null)
            {
                EnchantmentModel enchantmentModel = (EnchantmentModel)original.Enchantment.MutableClone();
                if (enchantmentModel.CanEnchant(cardModel))
                {
                    CardCmd.Enchant(enchantmentModel, cardModel, enchantmentModel.Amount);
                }
            }

            var transform = new CardTransformation(original, cardModel);
            transformations.Add(transform);
        }

        await CardCmd.Transform(transformations, null, CardPreviewStyle.GridLayout);
    }
}