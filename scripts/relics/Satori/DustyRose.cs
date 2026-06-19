using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(EventRelicPool))]
public class DustyRose : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<TheKoishiEye>();

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != base.Owner)
        {
            return;
        }

        if (player.Creature.CombatState == null) return;
        var koishiEye = player.Creature.CombatState.CreateCard<TheKoishiEye>(player);
        Flash();
        await CardPileCmd.AddGeneratedCardsToCombat([koishiEye], PileType.Hand, creator: base.Owner);
    }
}