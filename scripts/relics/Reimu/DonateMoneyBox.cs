using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 赛钱箱：回合开始时获得1能量。拾起时，将一张供奉加入牌组。
/// 供奉位于牌组中时，你持有的金币会被转化为供奉的进度。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class DonateMoneyBox : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientDonate),];
}
