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

    public override string? BgmFileName => "Kaguya.mp3";

    /// <summary>
    /// 辉夜曲不在开场播放，等「五道难题」释放后再开始。
    /// </summary>
    public override bool AutoStartBgm => false;

    // /// <summary>
    // /// 专属战斗背景：以 Map_Kaguya.png（1280×720）为主场景的背景场景。
    // /// </summary>
    // public override string? CustomBackgroundScenePath => "res://scenes/backgrounds/houraisan_kaguya/kaguya_background.tscn";

    public override string? CustomScenePath => "res://scenes/encounters/houraisan_kaguya.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<HouraisanKaguyaMonster>().ToMutable(), "kaguya")
    ];

    public HouraisanKaguyaEncounter() : base()
    {
    }

    public override bool IsValidForAct(ActModel act) => false;
}
