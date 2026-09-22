using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 至高之梦：每当你进入新的幕时，获得1瓶药水、50金币，最大生命提升5，
/// 获得1张牌，并随机升级1张牌。
///
/// 实现说明：
///   - 「获得1张牌」从当前角色的已解锁卡池中随机抽取（排除基础牌与诅咒牌），直接加入牌组。
///   - 「随机升级1张牌」从牌组中尚未升级且可升级的牌里随机抽取。
///   - 发奖励时地图已经打开，必须先把它关掉并在面板展示期间按住，详见 <see cref="AfterActEntered"/>。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SupremeDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Potions", 1),
        new GoldVar(50),
        new DynamicVar("MaxHpGain", 5),
        new CardsVar(1),
        new DynamicVar("Upgrades", 1)
    ];

    public override async Task AfterActEntered()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        // 进入新的幕时地图已经打开（MapScreen 是 OverlayScreensContainer 的后一个兄弟节点，绘制与输入
        // 都排在 overlay 之上），此时推入的奖励面板会被地图盖住、事件也被它吃掉，所以先关掉地图。
        // 只有本机玩家需要这么做（也只有他会看到这个面板），多人下就不必替远端关地图。
        var mapScreen = NMapScreen.Instance;
        bool ownsMapScreen = LocalContext.IsMe(player) && (mapScreen?.IsOpen ?? false);

        // 光关掉不够：NMapRoom 把 CapstoneClosed 连到了 ReopenMap，玩家按 ESC 再关闭暂停菜单时地图会被重开，
        // 把面板吞掉，而且地图的 SetTravelEnabled 会让未领取的奖励被直接跳过。
        // 所以连上 Opened，地图一被重开就立刻再关一次（Close 发出 Closed，NOverlayStack 借此把面板显示回来）。
        Callable mapReopenedGuard = Callable.From(CloseMapScreenWhileRewardsAreShown);
        if (ownsMapScreen)
        {
            mapScreen!.Close(animateOut: false);
            mapScreen.Connect(NMapScreen.SignalName.Opened, mapReopenedGuard);
        }

        try
        {
            Flash();
            // 提升最大生命
            var maxHpGain = base.DynamicVars["MaxHpGain"].BaseValue;
            if (maxHpGain > 0)
            {
                await CreatureCmd.GainMaxHp(player.Creature, maxHpGain);
            }

            CardCreationOptions options = new CardCreationOptions([Owner.Character.CardPool], CardCreationSource.Other, CardRarityOddsType.RegularEncounter);
            var rewards = new List<Reward>
            {
                new CardReward(options, 3, Owner),
                new GoldReward(base.DynamicVars.Gold.IntValue, base.DynamicVars.Gold.IntValue, base.Owner),
                new PotionReward(PotionFactory.CreateRandomPotionOutOfCombat(player, player.RunState.Rng.CombatPotionGeneration).ToMutable(),player)
            };

            await RewardsCmd.OfferCustom(base.Owner, rewards);
        }
        finally
        {
            // 地图是本幕唯一能继续往下走的入口，任何情况下都必须在收尾时恢复它。
            if (ownsMapScreen && mapScreen != null && GodotObject.IsInstanceValid(mapScreen))
            {
                mapScreen.Disconnect(NMapScreen.SignalName.Opened, mapReopenedGuard);
                mapScreen.Open();
            }
        }

        // 随机升级1张牌
        var upgradeCount = base.DynamicVars["Upgrades"].IntValue;
        for (int i = 0; i < upgradeCount; i++)
        {
            var upgradable = player.Deck.Cards
                .Where(c => c.IsUpgradable && !c.IsUpgraded)
                .ToList();

            if (upgradable.Count == 0) break;

            var target = upgradable[player.PlayerRng.Rewards.NextInt(0, upgradable.Count)];
            CardCmd.Upgrade(target);
        }
    }

    /// <summary>
    /// 奖励面板展示期间地图被重新打开时的兜底：立刻再关一次。
    /// <c>Close</c> 会发出 <c>Closed</c>，<c>NOverlayStack</c> 借此把面板重新显示出来。
    /// 注意：不能写成 lambda，否则 <c>Disconnect</c> 会因为 Callable 实例不同而失配。
    /// </summary>
    private static void CloseMapScreenWhileRewardsAreShown()
    {
        NMapScreen.Instance?.Close(animateOut: false);
    }
}
