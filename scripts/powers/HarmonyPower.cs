using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 协和：每回合开始获得 5 气氛，上回合完全挡下敌人攻击则额外获得 9。
/// </summary>
public sealed class HarmonyPower : TouhouAncientPowerModel
{
    private const int TurnAtmosphere = 5;
    private const int FullBlockAtmosphere = 9;

    private bool _tookUnblockedAttack;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("TurnAtmosphere", TurnAtmosphere),
        new DynamicVar("FullBlockAtmosphere", FullBlockAtmosphere),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientHyped)];

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner) return Task.CompletedTask;
        if (dealer == null || dealer.Side != CombatSide.Enemy) return Task.CompletedTask;
        if (!props.HasFlag(ValueProp.Move)) return Task.CompletedTask;

        if (result.UnblockedDamage > 0) _tookUnblockedAttack = true;
        return Task.CompletedTask;
    }

    // 必须用 AfterPlayerTurnStart：应援棒在 BeforeHandDraw 里选卡，而 AfterSideTurnStart 早于抽牌 → 第 1 回合会拿不到气氛
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner.Player) return;

        await KoishiPerformanceCmd.GainAtmosphere(choiceContext, player, TurnAtmosphere);
        if (!_tookUnblockedAttack)
        {
            await KoishiPerformanceCmd.GainAtmosphere(choiceContext, player, FullBlockAtmosphere);
        }

        _tookUnblockedAttack = false;
    }
}
