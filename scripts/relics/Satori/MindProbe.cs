using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 心灵探测仪：若敌人造成的伤害与你的格挡值相差绝对值小于等于2，使其下回合眩晕。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class MindProbe : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Threshold", 2m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [StunIntent.GetStaticHoverTip()];

    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner.Creature) return;
        if (dealer == null) return;
        if (!dealer.IsEnemy) return; // 只对敌人有效

        if (amount <= 0) return;

        var block = target.Block;
        var diff = Math.Abs(amount - block);

        if (diff <= 2m)
        {
            Flash();
            await CreatureCmd.Stun(dealer);
        }
    }
}
