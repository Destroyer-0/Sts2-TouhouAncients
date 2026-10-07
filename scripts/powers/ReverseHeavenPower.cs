using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.Afflictions;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 天地有用：战斗开始时，把所有玩家的基础打击/防御牌侵蚀为魔力躁动。
/// 本局中被移除的基础打击/防御牌由怪物侧在「妖器再塑」时视为叛乱之潮。
/// </summary>
public class ReverseHeavenPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    // 文本里用 {Cards:list:{}|、}：分隔符写在本地化文件里
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringListVar("Cards")];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromAffliction<ManaRestlessness>().Concat(HoverTipFactory.FromAffliction<RebellionTide>());

    /// <summary>刷新提示里的「仍可被塑成不语之物」的被移除牌（正邪每次召唤后由怪物侧调用）。</summary>
    public void SetAvailableRemovedCards(IReadOnlyList<string> titles)
    {
        ((StringListVar)base.DynamicVars["Cards"]).Set(titles);
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        foreach (var player in Owner.CombatState.Players.ToList())
        {
            foreach (CardModel card in player.PlayerCombatState.AllCards)
            {
                if (card.Affliction != null) continue;
                if (card.Rarity != CardRarity.Basic) continue;
                if (!card.Tags.Contains(CardTag.Strike) && !card.Tags.Contains(CardTag.Defend)) continue;

                // 永恒牌不受正邪的侵蚀影响
                if (card.Keywords.Contains(CardKeyword.Eternal)) continue;
                await CardCmd.Afflict<ManaRestlessness>(card, 1);
            }
        }
    }


    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Affliction != null) return;
        if (card.Rarity != CardRarity.Basic) return;
        if (!card.Tags.Contains(CardTag.Strike) && !card.Tags.Contains(CardTag.Defend)) return;
        if (card.Keywords.Contains(CardKeyword.Eternal)) return;

        await CardCmd.Afflict<ManaRestlessness>(card, 1);
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.CombatState == null)
            return Task.CompletedTask;
        foreach (Player player in oldOwner.CombatState.Players.ToList())
        {
            foreach (CardModel card in player.PlayerCombatState.AllCards.Where(c => c.Affliction is ManaRestlessness or RebellionTide))
                CardCmd.ClearAffliction(card);
        }

        return Task.CompletedTask;
    }
}