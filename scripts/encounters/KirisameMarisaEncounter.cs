using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

/// <summary>
/// 雾雨魔理沙挑战战斗。预置 6 个槽位：魔理沙本体 + 5 个召唤蘑菇位。
/// </summary>
public sealed class KirisameMarisaEncounter : TouhouAncientEncounter
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<KirisameMarisaMonster>(),
        ModelDb.Monster<FantasyMushroom>()
    ];

    /// <summary>预置槽位：魔理沙本体 + 5 个蘑菇位，供战斗中召唤使用。</summary>
    public override IReadOnlyList<string> Slots =>
    [
        "marisa", "mushroom1", "mushroom2", "mushroom3", "mushroom4", "mushroom5"
    ];

    public override string? BgmFileName => "TFM-010b_03.mp3";

    public override string? CustomScenePath => "res://scenes/encounters/kirisame_marisa.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<KirisameMarisaMonster>().ToMutable(), "marisa")
    ];

    public KirisameMarisaEncounter() : base(RoomType.Monster)
    {
    }

    public override bool IsValidForAct(ActModel act) => false;
}
