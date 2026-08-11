using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

/// <summary>
/// 博丽灵梦挑战战斗。单怪物槽位。
/// </summary>
public sealed class HakureiReimuEncounter : TouhouAncientEncounter
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<HakureiReimuMonster>()
    ];

    /// <summary>预置槽位：灵梦本体。</summary>
    public override IReadOnlyList<string> Slots =>
    [
        "reimu"
    ];

    public override string? BgmFileName => "TFM-Reimu.mp3";

    public override string? CustomScenePath => "res://scenes/encounters/hakurei_reimu.tscn";
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<HakureiReimuMonster>().ToMutable(), "reimu")
    ];

    public HakureiReimuEncounter() : base(RoomType.Monster)
    {
    }

    public override bool IsValidForAct(ActModel act) => false;
}
