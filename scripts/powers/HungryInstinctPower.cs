using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.Afflictions;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 饥渴本能：玩家身上的可消耗计数 Buff（Counter 类型，层数递减）。
/// 效果：接下来的 {Amount} 张打出的牌获得[消耗]；且每打出 1 张受影响的牌，
/// 饕餮尤魔增加当前最大生命 10% 的生命上限（上限 10 亿，超出后不再增加）。
/// </summary>
public class HungryInstinctPower : TouhouAncientPowerModel
{
    /// <summary>尤魔生命上限增长阈值（10 亿），达到后不再增加。</summary>
    private const decimal MaxMaxHp = 1_000_000_000m;

    /// <summary>每次触发增加的生命上限比例（10%）。</summary>
    private const decimal MaxHpGainPercent = 0.10m;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Amount", 0m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];


    private HashSet<CardModel> _affectedCards = new();

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _affectedCards = new HashSet<CardModel>();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (!Owner.IsPlayer || Owner.Player == null || Owner.Player.PlayerCombatState == null)
        {
            return;
        }

        List<CardModel> list = Owner.Player.PlayerCombatState.AllCards.ToList();
        foreach (CardModel item2 in list)
        {
            await Afflict(item2);
        }
    }

    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner != Owner.Player) return;
        
        if (card.Affliction == null)
        {
            CardType type = card.Type;
            await Afflict(card);
        }
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.CombatState == null)
        {
            return Task.CompletedTask;
        }

        if (oldOwner != base.Owner) return Task.CompletedTask;

        foreach (CardModel item2 in _affectedCards)
        {
            if (item2.HasBeenRemovedFromState) continue;
            if (item2.Affliction is not Devoured) continue;

            CardCmd.ClearAffliction(item2);
        }

        return Task.CompletedTask;
    }

    private async Task Afflict(CardModel card)
    {
        if (card.Affliction == null)
        {
            var devoured = await CardCmd.Afflict<Devoured>(card, base.Amount);
            if (devoured != null) _affectedCards.Add(card);
        }
    }

    /// <summary>
    /// 玩家打出牌后：若本 Power 还有剩余层数，则饕餮尤魔增加 10% 当前最大生命（上限 10 亿），层数 -1。
    /// 饥渴本能每触发一次只结算一次（用户确认：每层饥渴本能触发一次）。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (base.Amount <= 0) return;
        if (cardPlay.Card.Owner?.Creature != base.Owner) return;

        // 找到饕餮尤魔（本战斗中的尤魔本体）
        Creature? yuuma = base.Owner.CombatState?
            .Enemies
            .FirstOrDefault(c => c is { Monster: ToutetsuYuumaMonster, IsDead: false });
        if (yuuma != null && yuuma.MaxHp < MaxMaxHp)
        {
            decimal gain = System.Math.Floor(yuuma.MaxHp * MaxHpGainPercent);
            // 不超过上限
            decimal newMaxHp = yuuma.MaxHp + gain;
            if (newMaxHp > MaxMaxHp)
            {
                gain = MaxMaxHp - yuuma.MaxHp;
            }

            if (gain > 0m)
            {
                await CreatureCmd.GainMaxHp(yuuma, gain);
            }
        }

        Flash();
        await PowerCmd.Decrement(this);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}