using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

/// <summary>
/// 梅蒂欣·梅兰可莉挑战战斗。预置 2 个槽位：梅蒂欣本体 + 随从铃铃（位于左侧）。
/// </summary>
public sealed class MedicineMelancholyEncounter : TouhouAncientEncounter
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<MedicineMelancholyMonster>(),
        ModelDb.Monster<LingLingMonster>()
    ];

    /// <summary>预置槽位：梅蒂欣本体 + 铃铃（左侧随从）。</summary>
    public override IReadOnlyList<string> Slots =>
    [
        "medicine", "lingling"
    ];

    public override string? BgmFileName => null;

    public override string? CustomScenePath => "res://scenes/encounters/medicine_melancholy.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<MedicineMelancholyMonster>().ToMutable(), "medicine"),
        (ModelDb.Monster<LingLingMonster>().ToMutable(), "lingling")
    ];

    public MedicineMelancholyEncounter() : base(RoomType.Monster)
    {
    }

    public override bool IsValidForAct(ActModel act) => false;
}
