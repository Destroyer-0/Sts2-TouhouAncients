using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 赛钱箱：回合开始时获得1能量。拾起时，将一张供奉加入牌组。
/// 供奉位于牌组中时，你持有的金币会被转化为供奉的进度。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class MiniShrine : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Tribute>()];
    
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        // 将一张供奉加入牌组
        var tribute = player.RunState.CreateCard(ModelDb.Card<Tribute>(), player);
        await CardPileCmd.Add(tribute, PileType.Deck);

        // 立即将当前金币转化为供奉进度
        await TryDonate(player);
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }

    /// <summary>
    /// 获得金币后转化为供奉进度。
    /// </summary>
    public override async Task AfterGoldGained(Player player)
    {
        if (player != base.Owner) return;
        await TryDonate(player);
    }

    /// <summary>
    /// 尝试将持有金币转化为供奉进度。
    /// </summary>
    private async Task TryDonate(Player player)
    {
        // 只在非战斗进行中执行
        if (player.RunState.CurrentRoom is CombatRoom combatRoom 
            && combatRoom?.CombatState?.RunState.IsGameOver == true) return;

        // 在牌组中查找供奉牌
        var tribute = player.Deck.Cards.OfType<Tribute>();

        foreach (var card in tribute)
        {
            var remaining = card.RemainingGold;
            if (remaining <= 0) return;

            // 扣除至多剩余所需金币
            var donateAmount = Math.Min((int)player.Gold, remaining);
            if (donateAmount <= 0) return;

            // 扣金币
            Flash();
            await PlayerCmd.LoseGold(donateAmount, player);
            // 增加供奉进度
            await card.TryContribute(donateAmount);
        }

    }
}
