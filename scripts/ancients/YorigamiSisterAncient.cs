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
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.584f, 0.435f, 0.278f, 0.8f);
    public override Color DialogueColor => new(0.980f, 0.584f, 0.306f, 1f);

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
            AncientOption<ChanelHandbag>()
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