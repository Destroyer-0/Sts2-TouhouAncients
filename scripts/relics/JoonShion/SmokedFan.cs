using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 烟熏的团扇：所有精英敌人变为1生命并额外掉落1个普通遗物。
/// 每次击败精英，将一张羞耻加入牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SmokedFan : TouhouAncientRelics
{
    private bool _isEliteCombat;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("TargetHp", 1),
        //new DynamicVar("RelicNum", 1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Guilty>();

    /// <summary>
    /// 战斗开始时，判定是否为精英战斗。如果是，将所有敌人生命设为1。
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        var mapPoint = base.Owner.RunState.CurrentMapPoint;
        _isEliteCombat = mapPoint?.PointType == MapPointType.Elite;

        if (!_isEliteCombat) return;

        Flash();

        var enemies = base.Owner.Creature.CombatState.HittableEnemies;
        foreach (var enemy in enemies)
        {
            await CreatureCmd.SetCurrentHp(enemy, 1m);
        }
    }

    /// <summary>
    /// 战斗中后续加入的敌人同样处理。
    /// </summary>
    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (!_isEliteCombat) return;
        if (creature.Side != CombatSide.Enemy) return;

        Flash();
        await CreatureCmd.SetCurrentHp(creature, 1m);
    }
    //
    // /// <summary>
    // /// 修改战斗奖励：精英战斗额外掉落1个普通遗物。
    // /// </summary>
    // public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    // {
    //     if (player != base.Owner) return false;
    //     if (!_isEliteCombat) return false;
    //
    //     var commonRelic = RelicFactory.PullNextRelicFromFront(player, RelicRarity.Common).ToMutable();
    //     rewards.Add(new RelicReward(commonRelic, player));
    //     return true;
    // }

    /// <summary>
    /// 战斗结束后重置标记。
    /// </summary>
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (!_isEliteCombat) return;
        Flash();
        await CardPileCmd.AddCursesToDeck(Enumerable.Repeat(ModelDb.Card<Guilty>(), 1), Owner);
    }
}
