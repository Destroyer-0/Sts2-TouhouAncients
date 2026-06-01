using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;
[Pool(typeof(SharedRelicPool))]
public class JunkoTest: TouhouAncientRelics
{
    private int _goldenPathAct = -1;

    public override bool HasUponPickupEffect => true;

    [SavedProperty]
    public int GoldenPathAct
    {
        get
        {
            return _goldenPathAct;
        }
        set
        {
            AssertMutable();
            _goldenPathAct = value;
        }
    }

    public override async Task AfterObtained()
    {
        GoldenPathAct = base.Owner.RunState.CurrentActIndex;
        await RunManager.Instance.GenerateMap();
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
    ];
    
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }

    public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
    {
        if (GoldenPathAct != actIndex)
        {
            return map;
        }
        return new JunkoMapAct(runState);
    }

    public override IReadOnlySet<RoomType> ModifyUnknownMapPointRoomTypes(IReadOnlySet<RoomType> roomTypes)
    {
        if (GoldenPathAct != base.Owner.RunState.CurrentActIndex)
        {
            return roomTypes;
        }
        return new HashSet<RoomType> { RoomType.Event };
    }
}