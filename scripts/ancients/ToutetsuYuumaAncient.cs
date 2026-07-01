using Godot;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

[RegisterSharedAncient]
public class ToutetsuYuumaAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new(0.6f, 0.1f, 0.1f, 0.7f);
    public override Color DialogueColor => new(0.9f, 0.2f, 0.2f, 1f);

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

    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<BottomlessStomach>(),
        CreateModRelicOption<SkySwallowingSpoon>(),
        CreateModRelicOption<GuiltlessFace>(),
        CreateModRelicOption<GluttonousFang>(),
        CreateModRelicOption<GreedyEye>(),
        CreateModRelicOption<RigidDesireProof>(),
        CreateModRelicOption<CursedBlood>(),
        CreateModRelicOption<BloodlickingTongue>(),
        CreateModRelicOption<PurgatoryEmbers>(),
        CreateModRelicOption<EstrangedHeart>()
    ];
}