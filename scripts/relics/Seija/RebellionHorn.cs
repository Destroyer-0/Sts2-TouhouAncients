using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 反叛号角：拾起时，升级所有普通牌与初始牌。
/// 普通牌 = CardRarity.Common，初始牌 = CardRarity.Basic
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class RebellionHorn : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player == null) return;

        // 找出牌组中所有普通牌和初始牌
        var targets = player.Deck.Cards
            .Where(c => c.Rarity is CardRarity.Common or CardRarity.Basic)
            .ToList();

        if (targets.Count <= 0) return;

        Flash();
        foreach (var cardModel in targets)
        {
            CardCmd.Upgrade(cardModel);
        }
    }
}