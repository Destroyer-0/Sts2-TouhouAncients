using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(EventRelicPool))]
public class PureConfidence : TouhouAncientRelics
{
    private int _touhouAncientJunkoPathAct = -1;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.ForEnergy(this)];

    [SavedProperty]
    public int TouhouAncient_JunkoPathAct
    {
        get { return _touhouAncientJunkoPathAct; }
        set
        {
            AssertMutable();
            _touhouAncientJunkoPathAct = value;
        }
    }

    public override async Task AfterObtained()
    {
        TouhouAncient_JunkoPathAct = base.Owner.RunState.CurrentActIndex;
        await RunManager.Instance.GenerateMap();
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1)
    ];

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }
    
    public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
    {
        if (TouhouAncient_JunkoPathAct != actIndex)
        {
            return map;
        }
        return new JunkoMapAct(runState);
    }

    public override IReadOnlySet<RoomType> ModifyUnknownMapPointRoomTypes(IReadOnlySet<RoomType> roomTypes)
    {
        if (TouhouAncient_JunkoPathAct != base.Owner.RunState.CurrentActIndex)
        {
            return roomTypes;
        }

        return new HashSet<RoomType> { RoomType.Event };
    }
}