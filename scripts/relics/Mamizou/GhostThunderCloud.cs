using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 亡灵雷云：回合开始时，如果你上回合受到过来自敌人的伤害，
/// 获得 1 能量、抽 2 张牌并在本回合额外获得 3 力量。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class GhostThunderCloud : TouhouAncientRelics
{
    /// <summary>自上次我方回合开始以来，是否受到过敌人的未被格挡伤害。</summary>
    private bool _tookEnemyDamageSinceMyTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(2),
        new DynamicVar("Strength", 3m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner.Creature) return Task.CompletedTask;
        if (dealer == null || dealer.Side != CombatSide.Enemy) return Task.CompletedTask;
        if (props.HasFlag(ValueProp.Unpowered)) return Task.CompletedTask;
        if (result.UnblockedDamage <= 0) return Task.CompletedTask;

        _tookEnemyDamageSinceMyTurn = true;
        Status = RelicStatus.Active;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (!_tookEnemyDamageSinceMyTurn) return;
        _tookEnemyDamageSinceMyTurn = false;

        Flash();
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, player);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.IntValue, player);
        await PowerCmd.Apply<GhostThunderCloudStrengthPower>(choiceContext, player.Creature,
            base.DynamicVars["Strength"].BaseValue, player.Creature, null);
        Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _tookEnemyDamageSinceMyTurn = false;
        Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
