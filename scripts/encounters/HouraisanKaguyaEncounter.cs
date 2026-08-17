using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

/// <summary>
/// 蓬莱山辉夜挑战战斗。单怪物槽位。
/// </summary>
public sealed class HouraisanKaguyaEncounter : TouhouAncientEncounter
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<HouraisanKaguyaMonster>()
    ];

    /// <summary>预置槽位：辉夜本体。</summary>
    public override IReadOnlyList<string> Slots =>
    [
        "kaguya"
    ];

    public override string? BgmFileName => null;

    public override string? CustomScenePath => "res://scenes/encounters/houraisan_kaguya.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<HouraisanKaguyaMonster>().ToMutable(), "kaguya")
    ];

    public HouraisanKaguyaEncounter() : base(RoomType.Monster)
    {
    }

    public override bool IsValidForAct(ActModel act) => false;
}
