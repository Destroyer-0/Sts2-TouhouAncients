using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

public sealed class YorigamiSistersEncounter : CustomEncounterModel
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<YorigamiJoon>(),
        ModelDb.Monster<YorigamiShion>()
    ];

    public override bool IsValidForAct(ActModel act) => false;

    public override bool IsWeak => false;

    public YorigamiSistersEncounter() : base(RoomType.Monster)
    {
    }
}
