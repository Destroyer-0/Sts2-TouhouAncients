using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class YorigamiSisterAncient : TouhouAncientBase
{
    static YorigamiSisterAncient()
    {
        AncientSpeakerRegistry.RegisterProfiles(
            "TOUHOUANCIENTS-YORIGAMI_SISTER_ANCIENT",
            new Dictionary<string, AncientSpeakerProfile>
            {
                ["jyoon"] = new AncientSpeakerProfile(
                    "res://images/icon/Character/YorigamiSister_Joon.png",
                    "res://images/icon/Character/Outline/YorigamiSister_Joon.png",
                    new Color(0.980f, 0.584f, 0.306f, 1f)
                ),
                ["shion"] = new AncientSpeakerProfile(
                    "res://images/icon/Character/YorigamiSister_Shion.png",
                    "res://images/icon/Character/Outline/YorigamiSister_Shion.png",
                    new Color(0.4f, 0.3f, 0.7f, 1f)
                ),
            }
        );
    }

    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.584f, 0.435f, 0.278f, 0.8f);
    private Color JyoonButtonColor => new(0.980f, 0.584f, 0.306f, 0.8f);
    private Color ShionButtonColor => new(0.4f, 0.3f, 0.7f, 0.6f);
    public override Color DialogueColor => new(0.980f, 0.584f, 0.306f, 1f);

    public override Color GetOptionButtonColor(int optionIndex) => optionIndex switch
    {
        0 => ButtonColor,
        1 => JyoonButtonColor,
        2 => ShionButtonColor,
        _ => ButtonColor,
    };

    // /// <summary>
    // /// 重写 DefineDialogues：因为 JSON 键使用 .ancient_jyoon / .ancient_shion 变体后缀，
    // /// BaseLib 的扫描无法识别，所以手动构建对话结构。
    // /// </summary>
    // protected override AncientDialogueSet DefineDialogues()
    // {
    //     // ANY 对话结构（根据 JSON）：
    //     //   0: 3 lines, 非重复   (jyoon / shion / jyoon)
    //     //   1: 2 lines, 重复 1   (jyoon / shion)
    //     //   2: 3 lines, 重复 2   (jyoon / shion / jyoon)
    //     //   3: 1 line,  重复 3   (jyoon)
    //     var agnostic = new List<AncientDialogue>
    //     {
    //         new AncientDialogue("", "", ""),
    //         CreateRepeating(1, "", ""),
    //         CreateRepeating(2, "", "", ""),
    //         CreateRepeating(3, ""),
    //     };
    //
    //     // IRONCLAD 专有对话：
    //     //   0: 2 lines, 非重复   (shion / jyoon)
    //     var ironclad = new AncientDialogue[]
    //     {
    //         new AncientDialogue("", ""),
    //     };
    //
    //     return new AncientDialogueSet
    //     {
    //         FirstVisitEverDialogue = new AncientDialogue(""),
    //         CharacterDialogues = new Dictionary<string, IReadOnlyList<AncientDialogue>>
    //         {
    //             ["IRONCLAD"] = ironclad,
    //         },
    //         AgnosticDialogues = agnostic,
    //     };
    // }
    //
    // private static AncientDialogue CreateRepeating(int visitIndex, params string[] sfxPaths)
    // {
    //     var d = new AncientDialogue(sfxPaths);
    //     Traverse.Create(d).Property("VisitIndex").SetValue(visitIndex);
    //     return d;
    // }

    public override string? CustomMapIconPath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/WatariNina_MapNode.png";

    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/YorigamiSister.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/YorigamiSister.png";

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<PossessionSpirit>()
        ),
        MakePool(
            AncientOption<MillionPounds>(),
            AncientOption<PureGoldBracelet>(),
            AncientOption<ChanelHandbag>(),
            AncientOption<JyoonFan>()
        ),
        MakePool(
            AncientOption<BlackCatDoll>(),
            AncientOption<OilFutures>(),
            AncientOption<SmokedFan>(),
            AncientOption<GrilledMiso>()
        )
    );

    public override IEnumerable<EventOption> AllPossibleOptions
    {
        get
        {
            var currentOptions = base.AllPossibleOptions.ToList();
            for (int i = 0; i < currentOptions.Count; i++)
            {
                var option = currentOptions[i];
            }

            return base.AllPossibleOptions;
        }
    }
}