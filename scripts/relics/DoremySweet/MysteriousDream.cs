using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 深秘之梦：在每场战斗的第 N 回合开始时，将一张随机塔罗牌放入你的手牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MysteriousDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 4)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Tarot)];

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != base.Owner) return;
        if (player.PlayerCombatState?.TurnNumber != base.DynamicVars["Turns"].IntValue) return;

        Flash();

        // 同原版神话形态：共享 CombatCardSelection，联机各端抽到同一张
        var canonical = player.RunState.Rng.CombatCardSelection
            .NextItem(TarotCards.All.Where(card => card.IsAllowedInRun(player.RunState)));
        if (canonical == null) return;

        await CardPileCmd.AddGeneratedCardToCombat(
            combatState.CreateCard(canonical, player), PileType.Hand, creator: base.Owner);
    }
}
