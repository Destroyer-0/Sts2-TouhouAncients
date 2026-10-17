using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

/// <summary>渡里贝子挑战战斗。单怪物槽位。</summary>
public sealed class WatariNinaEncounter : TouhouAncientEncounter
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<WatariNinaMonster>()
    ];

    public override IReadOnlyList<string> Slots =>
    [
        "nina"
    ];

    public override string? BgmFileName => "Nina.mp3";
    public override string? CustomScenePath => "res://scenes/encounters/watari_nina.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<WatariNinaMonster>().ToMutable(), "nina")
    ];

    public WatariNinaEncounter() : base()
    {
    }

    public override bool IsValidForAct(ActModel act) => false;
}
