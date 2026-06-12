using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 舐血之舌：你每失去30生命，就获得10最大生命与3力量。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BloodlickingTongue : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";

    private const int Threshold = 30;

    [SavedProperty]
    public int TouhouAncients_TotalHpLost
    {
        get => totalHpLost;
        set
        {
            AssertMutable();
            totalHpLost = value;
            InvokeDisplayAmountChanged();
        }
    }

    private int totalHpLost;

    public override bool HasUponPickupEffect => false;
    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_TotalHpLost;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MaxHpVar(10),
        new DynamicVar("Strength", 3m),
        new DynamicVar("Threshold", Threshold),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
    ];

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner.Creature) return;
        int damageTaken = result.TotalDamage;
        if (damageTaken <= 0) return;

        int previousMilestone = TouhouAncients_TotalHpLost / Threshold;
        TouhouAncients_TotalHpLost += damageTaken;
        int newMilestone = TouhouAncients_TotalHpLost / Threshold;

        int triggers = newMilestone - previousMilestone;
        if (triggers <= 0) return;

        Flash();
        for (int i = 0; i < triggers; i++)
        {
            await CreatureCmd.GainMaxHp(base.Owner.Creature, DynamicVars["MaxHp"].IntValue);
            await PowerCmd.Apply<StrengthPower>(base.Owner.Creature, DynamicVars["Strength"].BaseValue, base.Owner.Creature, null);
        }
    }
}
