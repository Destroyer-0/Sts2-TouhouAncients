using System.Collections.Generic;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.encounters;

/// <summary>纯狐挑战战斗。单怪物槽位，二阶段由宙覆星合切换 BGM。</summary>
public sealed class JunkoEncounter : TouhouAncientEncounter
{
    /// <summary>二阶段 BGM（宙覆星合后接续）。</summary>
    public const string PhaseTwoBgmFileName = "Junko2.mp3";

    /// <summary>二阶段曲名显示键后缀（encounters 表）。</summary>
    private const string PhaseTwoBgmLocSuffix = ".bgm2";

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<JunkoMonster>()
    ];

    public override IReadOnlyList<string> Slots =>
    [
        "junko"
    ];

    public override string? BgmFileName => "Junko1.mp3";

    public override string? CustomScenePath => "res://scenes/encounters/junko.tscn";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<JunkoMonster>().ToMutable(), "junko")
    ];

    public JunkoEncounter() : base()
    {
    }

    public override bool IsValidForAct(ActModel act) => false;

    /// <summary>二阶段：切到 Junko2 并同步底部曲名（无 .bgm2 键时保留原曲名）。</summary>
    public void SwitchToPhaseTwoBgm()
    {
        EncounterBgm.Start(PhaseTwoBgmFileName, 2f);

        string? bgmName = LocString.GetIfExists("encounters", Id.Entry + PhaseTwoBgmLocSuffix)?.GetFormattedText();
        if (!string.IsNullOrEmpty(bgmName))
        {
            TouhouAncientEncounterBgmNamePatch.SetBgmName(bgmName);
        }
    }
}
