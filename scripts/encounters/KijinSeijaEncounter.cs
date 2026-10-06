using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

/// <summary>
/// 鬼人正邪挑战战斗。预置槽位：4 个不语之物位 + 正邪本体。
/// 不语之物槽位排在正邪之前，敌人回合按槽位顺序行动，因此衍生物先动、正邪最后动。
/// </summary>
public sealed class KijinSeijaEncounter : TouhouAncientEncounter
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<KijinSeijaMonster>(),
        ModelDb.Monster<SilentObjectMonster>()
    ];

    /// <summary>预置槽位：不语之物在前、正邪在后。</summary>
    public override IReadOnlyList<string> Slots =>
    [
        "silent1", "silent2", "silent3", "silent4", "seija"
    ];

    public override string? BgmFileName => "Seija.mp3";
    
    public override string? CustomScenePath => "res://scenes/encounters/kijin_seija.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<KijinSeijaMonster>().ToMutable(), "seija")
    ];

    public KijinSeijaEncounter() : base()
    {
    }

    public override bool IsValidForAct(ActModel act) => false;
}
