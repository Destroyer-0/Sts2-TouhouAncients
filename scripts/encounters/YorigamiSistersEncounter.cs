using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

public sealed class YorigamiSistersEncounter : CustomEncounterModel
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<YorigamiJoon>(),
        ModelDb.Monster<YorigamiShion>()
    ];

    public override string? CustomScenePath => "res://scenes/encounters/yorigami_sisters.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<YorigamiJoon>().ToMutable(), "joon"),
        (ModelDb.Monster<YorigamiShion>().ToMutable(), "shion"),
    ];

    public YorigamiSistersEncounter() : base(RoomType.Monster)
    {
    }

    /// <summary>
    /// 是否为挑战战斗（由 Ancient 事件的挑战选项进入）。挑战战斗不生成正常战斗奖励
    /// （金币/卡牌/药水），奖励由 Ancient 事件在战斗胜利后自行结算。
    /// 由 <see cref="TouhouAncientBase.StartChallenge"/> 在 ToMutable 后的实例上设置。
    /// </summary>
    public bool IsChallenge { get; set; }

    public override bool ShouldGiveRewards => !IsChallenge;

    public override bool IsValidForAct(ActModel act) => false;
}