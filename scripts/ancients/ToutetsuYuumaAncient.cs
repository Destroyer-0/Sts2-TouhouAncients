using Godot;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.encounters;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class ToutetsuYuumaAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new(0.6f, 0.1f, 0.1f, 0.7f);
    public override Color DialogueColor => new(0.9f, 0.2f, 0.2f, 1f);

    /// <summary>饕餮尤魔挑战战斗。</summary>
    public override TouhouAncientEncounter? ChallengeEncounter => ModelDb.Encounter<ToutetsuYuumaEncounter>();

    public override string? CustomMapIconPath => "res://images/icon/MapNode/ToutetsuYuuma_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/ToutetsuYuuma_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/ToutetsuYuuma.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/ToutetsuYuuma.png";

    protected override AncientDialogueSet DefineDialogues()
    {
        var dialogs = base.DefineDialogues();
        var keys = dialogs.CharacterDialogues.Keys.ToList();
        foreach (var characterDialogue in keys)
        {
            var list = dialogs.CharacterDialogues[characterDialogue];
            var list2 = new List<AncientDialogue>();
            var index = 5;
            if (list.Count >= 1)
            {
                var sfxPathLength = list[0].Lines.Count;
                list2.Add(new AncientDialogue(Enumerable.Repeat("", sfxPathLength).ToArray()) { VisitIndex = 0 });
            }

            for (var i = 1; i < list.Count; i++)
            {
                var sfxPathLength = list[i].Lines.Count;
                list2.Add(new AncientDialogue(Enumerable.Repeat("", sfxPathLength).ToArray())
                    { VisitIndex = index + i - 1 });
            }

            dialogs.CharacterDialogues[characterDialogue] = list2;
        }

        return new AncientDialogueSet
        {
            FirstVisitEverDialogue = dialogs.FirstVisitEverDialogue,
            CharacterDialogues = dialogs.CharacterDialogues,
            AgnosticDialogues = new[]
            {
                new AncientDialogue(""),
                new AncientDialogue("") { VisitIndex = 1 },
                new AncientDialogue("") { VisitIndex = 2 },
                new AncientDialogue("") { VisitIndex = 3 },
                new AncientDialogue("") { VisitIndex = 4 },
                new AncientDialogue(""),
            }
        };
    }

    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        CreateTARelicOptionPool(
            TARelicOption<BottomlessStomach>(),
            TARelicOption<SkySwallowingSpoon>(),
            TARelicOption<GuiltlessFace>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<GluttonousFang>(),
            TARelicOption<GreedyEye>(),
            TARelicOption<RigidDesireProof>(),
            TARelicOption<CursedBlood>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<BloodlickingTongue>(),
            TARelicOption<PurgatoryEmbers>(),
            TARelicOption<EstrangedHeart>()
            )
    ];
}