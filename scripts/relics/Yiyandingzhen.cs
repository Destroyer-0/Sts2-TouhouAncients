using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.TestSupport;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class Yiyandingzhen: TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [(HoverTipFactory.FromPower<ConfusedPower>())];
    
    public override async Task AfterObtained()
    {
        if (CombatManager.Instance.IsInProgress)
        {
            await ApplyPower();
        }
    }

    public override async Task BeforeCombatStart()
    {
        await ApplyPower();
    }

    private async Task ApplyPower()
    {
        await PowerCmd.Apply<ConfusedPower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        Flash();
        var selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
            context: choiceContext,
            player: player,
            filter: null,
            source: this)).FirstOrDefault();
        selected?.SetToFreeThisTurn();
    }
}