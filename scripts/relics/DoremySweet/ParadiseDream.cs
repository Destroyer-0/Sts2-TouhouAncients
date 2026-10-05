using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 乐园之梦：拾起时，将一张「妖怪治退」加入你的牌组。
/// 击败第一幕的 Boss 后，将此牌从牌组中移除，其每有 1 点伤害就获得 10 金币。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class ParadiseDream : TouhouAncientRelics
{
    [SavedProperty]
    public bool ParadiseDream_Resolved { get; set; }

    public override bool HasUponPickupEffect => true;

    /// <summary>每 1 点伤害对应的金币。</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(10)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<YoukaiExtermination>();

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var card = player.RunState.CreateCard<YoukaiExtermination>(player);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
    }

    /// <summary>击败第一幕 Boss 后：移除妖怪治退，按其当前伤害每点换 10 金币。</summary>
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (ParadiseDream_Resolved) return;
        if (room.RoomType != RoomType.Boss) return;
        if (base.Owner.RunState.CurrentActIndex != 0) return; // 第一幕

        ParadiseDream_Resolved = true;

        var card = base.Owner.Deck.Cards.OfType<YoukaiExtermination>().FirstOrDefault();
        if (card == null) return;

        int damage = card.CurrentDamage;
        await CardPileCmd.RemoveFromDeck(card);
        if (damage > 0)
        {
            await PlayerCmd.GainGold(damage * base.DynamicVars.Gold.BaseValue, base.Owner);
        }
    }
}
