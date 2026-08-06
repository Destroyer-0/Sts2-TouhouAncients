using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

public sealed class YorigamiSistersEncounter : TouhouAncientEncounter
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<YorigamiJoonMonster>(),
        ModelDb.Monster<YorigamiShionMonster>()
    ];

    /// <summary>本挑战战斗的自定义 BGM（位于 res://debug_audio/，import 已开启 loop）。</summary>
    public override string? BgmFileName => "TFM-Yorigami.mp3";

    public override string? CustomScenePath => "res://scenes/encounters/yorigami_sisters.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<YorigamiJoonMonster>().ToMutable(), "joon"),
        (ModelDb.Monster<YorigamiShionMonster>().ToMutable(), "shion"),
    ];

    public YorigamiSistersEncounter() : base(RoomType.Monster)
    {
    }

    public override bool IsValidForAct(ActModel act) => false;
}