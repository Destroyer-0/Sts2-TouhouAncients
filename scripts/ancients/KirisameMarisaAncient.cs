using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Events;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Entities.Ancients;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

/// <summary>
/// 先古之民：雾雨魔理沙（Kirisame Marisa�?/// 普通的黑魔术少女，使用八卦炉和扫帚的魔法使�?/// </summary>
[RegisterSharedAncient]
public class KirisameMarisaAncient : TouhouAncientBase
{
    public override int? ShowAct => null;
    public override Color ButtonColor => new(0.3f, 0.3f, 0.3f, 0.7f);
    public override Color DialogueColor => new(0.9f, 0.6f, 0.1f, 1f);



    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<MiniHakkero>(),
        CreateModRelicOption<LoveColorFlashlight>(),
        CreateModRelicOption<CometAccelerator>(),
        CreateModRelicOption<KompeitoPot>(),
        CreateModRelicOption<StardustBroom>(),
        CreateModRelicOption<WitchsCauldron>(),
        CreateModRelicOption<BottledGalaxy>(),
        CreateModRelicOption<UnstableBottle>(),
        CreateModRelicOption<Globe>(),
        CreateModRelicOption<MushroomBento>()
    ];
}
