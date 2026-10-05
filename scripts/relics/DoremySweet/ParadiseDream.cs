using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
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

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (ParadiseDream_Resolved) return false;
        if (room.RoomType != RoomType.Boss) return false;
        if (base.Owner.RunState.CurrentActIndex != 0) return false; // 第一幕

        ParadiseDream_Resolved = true;

        var cards = base.Owner.Deck.Cards.OfType<YoukaiExtermination>();

        var trigger = false;
        foreach (var card in cards)
        {
            trigger = true;
            // 取面板值：含锋利等附魔修正；牌在牌组里、战斗外，不会跑全局伤害 Hook
            card.UpdateDynamicVarPreview(CardPreviewMode.None, null, card.DynamicVars);
            int damage = (int)card.DynamicVars.Damage.PreviewValue;
            Flash();
            if (damage > 0)
            {
                rewards.Add(new GoldReward(damage * base.DynamicVars.Gold.IntValue, base.Owner));
            }
        }

        return trigger;
    }

    public override async Task AfterModifyingRewards()
    {
        var cards = base.Owner.Deck.Cards.OfType<YoukaiExtermination>();
        await CardPileCmd.RemoveFromDeck(cards.ToList());
    }

}
