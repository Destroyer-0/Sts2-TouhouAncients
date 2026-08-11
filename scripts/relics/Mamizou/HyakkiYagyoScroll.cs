using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 百鬼夜行绘卷：拾起时，查看来自铁甲战士、静默猎者、储君、亡灵契约师、故障机器人的
/// 各{CardPacks}组卡牌奖励。将选择的卡牌奖励合成为卡牌：百鬼夜行加入你的牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class HyakkiYagyoScroll : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardPacks", 1m),
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<HyakkiYagyo>();
}
