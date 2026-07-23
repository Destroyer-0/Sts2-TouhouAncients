using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 朱莉安娜羽扇：非精英的战斗开始时，获得75金币、2力量、2敏捷、2集中，
/// 这个遗物在4场非精英战斗后失效，每次击败精英后增加4充能。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class JyoonFan : TouhouAncientRelics
{
    [SavedProperty]
    public int TouhouAncients_Charges
    {
        get => charges;
        set
        {
            AssertMutable();
            charges = value;
            InvokeDisplayAmountChanged();
        }
    }

    private int charges;

    private int ChargeAmount => base.DynamicVars["Charges"].IntValue;

    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_Charges;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(50),
        new DynamicVar("Strength", 2m),
        new DynamicVar("Dexterity", 2m),
        new DynamicVar("Focus", 2m),
        new DynamicVar("Charges", 4m),
    ];

    public override async Task AfterObtained()
    {
        TouhouAncients_Charges = ChargeAmount;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<FocusPower>(),
    ];

    public override async Task BeforeCombatStart()
    {
        var mapPoint = base.Owner.RunState.CurrentMapPoint;
        if (mapPoint?.PointType == MapPointType.Elite)
            return;

        if (TouhouAncients_Charges <= 0)
            return;

        Flash();
        await PlayerCmd.GainGold(base.DynamicVars.Gold.IntValue, base.Owner);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Owner.Creature,
            base.DynamicVars["Strength"].BaseValue, base.Owner.Creature, null);
        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), base.Owner.Creature,
            base.DynamicVars["Dexterity"].BaseValue, base.Owner.Creature, null);
        await PowerCmd.Apply<FocusPower>(new ThrowingPlayerChoiceContext(), base.Owner.Creature,
            base.DynamicVars["Focus"].BaseValue, base.Owner.Creature, null);

        TouhouAncients_Charges--;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        var mapPoint = base.Owner.RunState.CurrentMapPoint;
        if (mapPoint?.PointType == MapPointType.Elite)
        {
            TouhouAncients_Charges += ChargeAmount;
        }
    }
}
