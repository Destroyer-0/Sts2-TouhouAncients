using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 金平糖罐：你的卡牌奖励中将额外出现一张升级过的能力牌，选取3张后失效。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class KompeitoPot : TouhouAncientRelics
{
    private const int MaxUses = 3;

    [SavedProperty]
    public int TouhouAncients_UsesRemaining { get; set; } = MaxUses;

    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_UsesRemaining;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("UsesRemaining", MaxUses)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        Enumerable.Empty<IHoverTip>();

    public override bool HasUponPickupEffect => false;

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> options,
        CardCreationOptions creationOptions)
    {
        if (base.Owner != player) return false;
        if (TouhouAncients_UsesRemaining <= 0) return false;
        if (creationOptions.Source != CardCreationSource.Encounter) return false;

        // 找一张能力牌
        var powerCards = creationOptions.GetPossibleCards(player)
            .Where(c => c.Type == CardType.Power && c.Rarity != CardRarity.Curse)
            .ToList();

        if (!powerCards.Any()) return false;

        var rng = player.PlayerRng.Rewards;
        var chosen = powerCards.UnstableShuffle(rng).First();

        Flash();

        // 克隆并升级
        var cloned = base.Owner.RunState.CloneCard(chosen);
        CardCmd.Upgrade(cloned);

        var result = new CardCreationResult(cloned);
        result.ModifyCard(cloned, this);
        options.Add(result);

        TouhouAncients_UsesRemaining--;
        InvokeDisplayAmountChanged();

        if (TouhouAncients_UsesRemaining <= 0)
        {
            base.Status = MegaCrit.Sts2.Core.Entities.Relics.RelicStatus.Disabled;
        }

        return true;
    }
}
