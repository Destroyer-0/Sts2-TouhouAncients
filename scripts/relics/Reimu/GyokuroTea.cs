using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 玉露茶：你每抽 9 张牌，获得 1 能量。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class GyokuroTea : TouhouAncientRelics
{
    [SavedProperty] public int TouhouAncients_CardsDrawn { get; set; }

    public override bool ShowCounter => true;

    public override int DisplayAmount => TouhouAncients_CardsDrawn % DynamicVars.Cards.IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(9),
        new EnergyVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner != base.Owner) return;

        TouhouAncients_CardsDrawn++;
        UpdateDisplay();

        if (TouhouAncients_CardsDrawn % DynamicVars.Cards.IntValue != 0) return;

        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, base.Owner);
    }

    private void UpdateDisplay()
    {
        int threshold = DynamicVars.Cards.IntValue;
        // 接近触发时高亮，和原版无常鬼彻（Nunchaku）一致
        base.Status = TouhouAncients_CardsDrawn % threshold == threshold - 1
            ? RelicStatus.Active
            : RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }
}
