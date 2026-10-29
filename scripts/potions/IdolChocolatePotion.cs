using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.PotionPools;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.potions;

/// <summary>
/// 特制造型巧克力：非嗨到爆时进入嗨到爆；已在嗨到爆则随机打出抽牌堆与弃牌堆里的 3 张牌。
/// </summary>
[Pool(typeof(TokenPotionPool))]
public sealed class IdolChocolatePotion : CustomPotionModel
{
    private const int AutoPlayCount = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("AutoPlayCards", AutoPlayCount)];

    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    public override string? CustomPackedImagePath => $"res://images/potion/{GetType().Name}.png";
    public override string? CustomPackedOutlinePath => $"res://images/potion/{GetType().Name}_outline.png";

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        if (!KoishiPerformanceCmd.IsHyped(player))
        {
            await KoishiPerformanceCmd.EnterHyped(choiceContext, player);
            return;
        }

        var cards = PileType.Draw.GetPile(player).Cards
            .Concat(PileType.Discard.GetPile(player).Cards)
            .ToList();
        var picks = cards.UnstableShuffle(player.RunState.Rng.CombatCardGeneration)
            .Take(AutoPlayCount)
            .ToList();

        foreach (var card in picks)
        {
            if (CombatManager.Instance.IsOverOrEnding) break;
            await CardCmd.AutoPlay(choiceContext, card, null);
        }
    }
}
