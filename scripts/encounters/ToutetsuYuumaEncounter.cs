using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

/// <summary>
/// 饕餮尤魔挑战战斗（先古之民）。预置 1 个槽位：尤魔本体。
/// </summary>
public sealed class ToutetsuYuumaEncounter : TouhouAncientEncounter
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<ToutetsuYuumaMonster>()
    ];

    /// <summary>预置槽位：尤魔本体。</summary>
    public override IReadOnlyList<string> Slots =>
    [
        "yuuma"
    ];

    public override float GetCameraScaling()
    {
        return 0.9f;
    }

    public override string? BgmFileName => "Yuuma.mp3";

    public override string? CustomScenePath => "res://scenes/encounters/toutetsu_yuuma.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<ToutetsuYuumaMonster>().ToMutable(), "yuuma")
    ];

    public ToutetsuYuumaEncounter() : base()
    {
    }

    public override bool IsValidForAct(ActModel act) => false;
}
