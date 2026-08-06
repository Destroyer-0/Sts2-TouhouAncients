using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.encounters;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

/// <summary>
/// 先古之民：雾雨魔理沙（Kirisame Marisa）
/// 普通的黑魔术少女，使用八卦炉和扫帚的魔法使。
/// </summary>
public class KirisameMarisaAncient : TouhouAncientBase
{
    public override int? ShowAct => null;
    public override Color ButtonColor => new(0.3f, 0.3f, 0.3f, 0.7f);
    public override Color DialogueColor => new(0.9f, 0.6f, 0.1f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/KirisameMarisa_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/KirisameMarisa_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/KirisameMarisa.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/KirisameMarisa.png";
    
    public override TouhouAncientEncounter? ChallengeEncounter => ModelDb.Encounter<KirisameMarisaEncounter>();
    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<MiniHakkero>(),
            AncientOption<LoveColorFlashlight>(),
            AncientOption<CometAccelerator>()
        ),
        MakePool(
            AncientOption<KompeitoPot>(),
            AncientOption<StardustBroom>(),
            AncientOption<WitchsCauldron>(),
            AncientOption<BottledGalaxy>()
        ),
        MakePool(
            AncientOption<UnstableBottle>(),
            AncientOption<Globe>(),
            AncientOption<MushroomBento>()
        ));
}
