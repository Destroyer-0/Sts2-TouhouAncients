// using BaseLib.Abstracts;
// using BaseLib.Extensions;
// using BaseLib.Utils;
// using Godot;
// using MegaCrit.Sts2.Core.Models;
// using TouhouAncients.Scripts.relics;
//
// namespace TouhouAncients.Scripts;
//
// /// <summary>
// /// 先古之民：二岩猯藏（Futatsuiwa Mamizou）
// /// 佐渡的二枚岩大狸子，擅长变化的妖怪狸猫。
// /// 出现在二层或三层。
// /// </summary>
// public class FutatsuiwaMamizouAncient : TouhouAncientBase
// {
//     public override int? ShowAct => null;
//     public override Color ButtonColor => new(0.545f, 0.353f, 0.169f, 0.7f);
//     public override Color DialogueColor => new(0.545f, 0.353f, 0.169f, 1f);
//
//     public override string? CustomMapIconPath => "res://images/icon/MapNode/HakureiReimu_MapNode.png";
//     public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/HakureiReimu_MapNode.png";
//
//     public override string? CustomRunHistoryIconPath => "res://images/icon/Character/HakureiReimu.png";
//     public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/HakureiReimu.png";
//
//     protected override OptionPools MakeOptionPools => new OptionPools(
//         MakePool(
//             AncientOption<ReliableTanukiDisciple>(),
//             AncientOption<CrimsonCloudKnuckles>(),
//             AncientOption<SunkenAnchorGhost>(),
//             AncientOption<NidomiteiSpecialBlend>()
//         ),
//         MakePool(
//             AncientOption<PsychicTelekinesis>(),
//             AncientOption<WickedHermitHairpin>(),
//             AncientOption<DowsingRod>(),
//             AncientOption<DragonVeinVessel>()
//         ),
//         MakePool(
//             AncientOption<DisillusionTrident>(),
//             AncientOption<OneEyedKarakasa>(),
//             AncientOption<HyakkiYagyoScroll>(),
//             AncientOption<NohMask>()
//         )
//     );
// }
