using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 赤云指虎：当你不以自动打出的方式打出攻击牌时，随机打出手牌中另一张攻击牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class CrimsonCloudKnuckles : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (cardPlay.IsAutoPlay) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        var player = base.Owner;
        if (player.PlayerCombatState == null) return;

        // 从手牌中找另一张攻击牌（排除刚打出的这张）
        var handAttacks = player.PlayerCombatState.Hand.Cards
            .Where(c => c != cardPlay.Card && c.Type == CardType.Attack)
            .ToList();

        if (handAttacks.Count == 0) return;

        Flash();

        var target = handAttacks[base.Owner.RunState.Rng.CombatTargets.NextInt(handAttacks.Count)];
        await CardCmd.AutoPlay(context, target, null);
    }
}
