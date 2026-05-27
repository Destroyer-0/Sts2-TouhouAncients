using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 不稳定魔瓶：在每个回合开始时，随机为抽牌堆中的两张牌附魔华彩（GLAM）。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class UnstableBottle : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<Glam>();

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != base.Owner) return;
        if (base.Owner.PlayerCombatState == null) return;

        var drawPile = base.Owner.PlayerCombatState.DrawPile;
        if (drawPile.IsEmpty) return;

        var glam = ModelDb.Enchantment<Glam>().ToMutable();
        var candidates = drawPile.Cards
            .Where(c => glam.CanEnchant(c))
            .ToList();

        if (candidates.Count == 0) return;

        var toEnchant = candidates
            .UnstableShuffle(base.Owner.RunState.Rng.CombatCardSelection)
            .Take(Math.Min(2, candidates.Count))
            .ToList();

        if (toEnchant.Count == 0) return;

        Flash();

        foreach (var card in toEnchant)
        {
            CardCmd.Enchant<Glam>(card, 1m);
            CardCmd.Preview(card);
        }
    }
}
