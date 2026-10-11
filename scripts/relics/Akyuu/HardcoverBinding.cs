using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 精装封皮：你在非战斗房间中获得的卡牌将自动升级。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class HardcoverBinding : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /// <summary>不改牌，直接就地升级（<see cref="CardPreviewStyle.None"/> 不带 VFX）。</summary>
    public override bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)
    {
        newCard = null;
        if (card.Owner != base.Owner) return false;
        if (!card.IsUpgradable || card.IsUpgraded) return false;
        if (base.Owner.RunState.CurrentRoom is CombatRoom) return false;

        CardCmd.Upgrade(card, CardPreviewStyle.None);
        return false;
    }
}
