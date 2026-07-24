using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
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

    public override string? CustomScenePath => "res://scenes/encounters/yorigami_sisters.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<YorigamiJoon>().ToMutable(), "joon"),
        (ModelDb.Monster<YorigamiShion>().ToMutable(), "shion"),
    ];

    public YorigamiSistersEncounter() : base(RoomType.Monster)
    {
    }

    public override bool IsValidForAct(ActModel act) => false;
}