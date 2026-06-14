using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class ToutetsuYuumaAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.6f, 0.1f, 0.1f, 0.7f);
    public override Color DialogueColor => new(0.9f, 0.2f, 0.2f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/ToutetsuYuuma.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/ToutetsuYuuma.png";

    public override bool IsValidForAct(ActModel act)
    {
        return act.ActNumber() == 3;
    }

    protected override AncientDialogueSet DefineDialogues()
    {
        var dialogs = base.DefineDialogues();
        var keys = dialogs.CharacterDialogues.Keys.ToList();
        var universal = dialogs.AgnosticDialogues;
        foreach (var characterDialogue in keys)
        {
            var list = dialogs.CharacterDialogues[characterDialogue];
            var list2 = new List<AncientDialogue>(list);
            var index = Mathf.Max(1, list2.Count);
            if (list2.Count == 0)
            {
                list2.Add(universal[1]);
                list2.Add(universal[2]);
                list2.Add(universal[3]);
                list2.Add(universal[4]);
            }
            else
            {
                list2.Insert(index++, universal[1]);
                list2.Insert(index++, universal[2]);
                list2.Insert(index++, universal[3]);
                list2.Insert(index, universal[4]);
            }
            dialogs.CharacterDialogues[characterDialogue] = list2;
        }
        return dialogs;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<ToutetsuYuumaAncient>(act.ActNumber());
    }

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<BottomlessStomach>(),
            AncientOption<SkySwallowingSpoon>(),
            AncientOption<GuiltlessFace>()
        ),
        MakePool(
            AncientOption<GluttonousFang>(),
            AncientOption<GreedyEye>(),
            AncientOption<RigidDesireProof>(),
            AncientOption<CursedBlood>()
        ),
        MakePool(
            AncientOption<BloodlickingTongue>(),
            AncientOption<PurgatoryEmbers>(),
            AncientOption<EstrangedHeart>())
    );
}