using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

/// <summary>
/// 因幡帝挑战战斗（先古之民）。预置 1 个槽位：因幡帝本体。
/// </summary>
public sealed class InabaTewiEncounter : TouhouAncientEncounter
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<InabaTewiMonster>()
    ];

    /// <summary>预置槽位：因幡帝本体。</summary>
    public override IReadOnlyList<string> Slots =>
    [
        "tewi"
    ];

    public override string? BgmFileName => "Tewi.mp3";

    public override string? CustomScenePath => "res://scenes/encounters/inaba_tewi.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<InabaTewiMonster>().ToMutable(), "tewi")
    ];

    public InabaTewiEncounter() : base()
    {
    }

    public override bool IsValidForAct(ActModel act) => false;
}
