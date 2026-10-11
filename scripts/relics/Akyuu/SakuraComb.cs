using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 樱色发梳：每当你进入休息处或遇到先古之民时，获得5点最大生命，
/// 然后将一张阎罗之罪罚加入你的牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SakuraComb : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<YamaPunishment>();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(5m)];

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not RestSiteRoom && room is not EventRoom { CanonicalEvent: AncientEventModel }) return;

        Flash();
        await CreatureCmd.GainMaxHp(base.Owner.Creature, base.DynamicVars.MaxHp.BaseValue);

        var card = base.Owner.RunState.CreateCard<YamaPunishment>(base.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }
}
