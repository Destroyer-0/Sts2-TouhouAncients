using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 血煞祸劫：饕餮尤魔的成长诅咒（不可叠加，挂在尤魔身上）。
/// 尤魔每拥有 1 点力量，受到伤害 / 失去生命 / 被施加灾厄层数就翻倍一次（×2^力量），
/// 倍率上限 999999 倍（本地化描述不写明上限，仅代码钳制）。
/// 玩家通过"欲壑血海"给尤魔喂力量，使输出成指数增长，是击杀 300 万 HP 的核心机制。
/// </summary>
public class BloodDisasterPower : TouhouAncientPowerModel
{
    /// <summary>倍率上限（隐藏，不写入本地化）。</summary>
    private const decimal MaxMultiplier = 999999m;

    public int CurrentStrength
    {
        get
        {
            int strength;
            try
            {
                strength = base.Owner.GetPower<StrengthPower>()?.Amount ?? 0;
            }
            catch (System.InvalidOperationException)
            {
                // canonical / 未绑定战斗状态：无力量
                return 0;
            }

            return strength;
        }
    }

    /// <summary>
    /// 伤害/失去生命/灾厄的翻倍倍率 = 2^力量，钳制在 <see cref="MaxMultiplier"/>。
    /// 力量为 0 时返回 1（不翻倍）。canonical 实例（无 Owner）返回 1。
    /// </summary>
    public decimal CurrentMultiplier
    {
        get
        {
            if (CurrentStrength <= 0) return 1m;
            if (CurrentStrength >= 20) return MaxMultiplier; // 2^20 = 1048576，超过上限
            return Math.Min(MaxMultiplier, (decimal)Math.Pow(2, CurrentStrength));
        }
    }

    public override PowerType Type => PowerType.None;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)CurrentMultiplier;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DoomPower>(),
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ChangeAmount();
        return base.AfterApplied(applier, cardSource);
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner) return 1;
        return CurrentMultiplier;
    }

    /// <summary>
    /// 被施加灾厄时层数翻倍。仅当目标为本能力持有者（尤魔）且被施加的是灾厄（DoomPower）时生效。
    /// </summary>
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (target != base.Owner) return false;
        if (canonicalPower is not DoomPower) return false;
        decimal multiplier = CurrentMultiplier;
        if (multiplier == 1m) return false;
        modifiedAmount = amount * multiplier;
        return true;
    }

    /// <summary>
    /// 层数变化时刷新显示（DynamicVars["Multiplier"]）。
    /// 尤魔力量变化（StrengthPower）时也要刷新，因为翻倍倍率随力量实时变化。
    /// </summary>
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (power == this || power is StrengthPower)
        {
            ChangeAmount();
            //base.DynamicVars["Multiplier"].BaseValue = CurrentMultiplier;
        }
    }

    private void ChangeAmount()
    {
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(base.Owner);
        float scale = 1f + CurrentStrength * 0.1f;
        node?.ScaleTo(scale, 0f);
        InvokeDisplayAmountChanged();
        if (node == null || Owner.Monster is not ToutetsuYuumaMonster)
        {
            return;
        }

        float visualScale = scale * node.Visuals.DefaultScale;
        node.IntentContainer.Position = node.Body.Position * visualScale + new Vector2(0f, -270f - CurrentStrength * 15f) - node.IntentContainer.Size / 2f;
    }
}