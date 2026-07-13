using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
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
                    "res://images/icon/Character/YorigamiSister.png",
                    "res://images/icon/Character/Outline/YorigamiSister.png",
                    new Color(0.980f, 0.584f, 0.306f, 1f)
                ),
                ["shion"] = new AncientSpeakerProfile(
                    "res://images/icon/Character/YorigamiSister_Shion.png",
                    "res://images/icon/Character/Outline/YorigamiSister.png",
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