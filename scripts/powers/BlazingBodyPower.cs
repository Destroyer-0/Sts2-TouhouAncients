using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 炽火灼身：辉夜施加给玩家的负面能力。
/// 在持有者回合开始时，将 Amount 张灼伤加入持有者的弃牌堆。
/// </summary>
public sealed class BlazingBodyPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>悬停提示：显示灼伤卡。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Burn>();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 1m)];

    /// <summary>
    /// 玩家回合开始时，将 Amount 张灼伤加入持有者的弃牌堆。
    /// </summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(base.Owner))
        {
            return;
        }
        await CardPileCmd.AddToCombatAndPreview<Burn>(base.Owner, PileType.Discard, base.Amount, base.Owner.Player, CardPilePosition.Random);
    }
}
