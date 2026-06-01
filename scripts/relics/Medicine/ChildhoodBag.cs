using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

[HarmonyPatch(typeof(FoulPotion))]
public static class ChildhoodBagPatch
{
    [HarmonyPatch("OnUse")] 
    [HarmonyPrefix]
    private static bool Prefix(FoulPotion __instance, PlayerChoiceContext choiceContext, Creature? target)
    {
        // 仅在战斗中拦截，且需要替换逻辑时返回 false
        if (!CombatManager.Instance.IsInProgress) return true;

        var player = __instance.Owner;
        if (player == null) return true;

        // 只有持有 ChildhoodBag 才需要替换逻辑
        if (player.GetRelic<ChildhoodBag>() != null)
        {
            // 启动异步任务但让原方法正常执行，然后跳过原方法的逻辑
            _ = ReplaceOnUseLogic(__instance, choiceContext, target);
            return false; // 跳过原方法
        }

        return true; // 继续原方法
    }

// 异步逻辑单独提取
    private static async Task ReplaceOnUseLogic(FoulPotion __instance, PlayerChoiceContext choiceContext,
        Creature? target)
    {
        try
        {
            var player = __instance.Owner;
            if (player == null) return;

            var dealer = player.Creature;
            var damage = __instance.DynamicVars.Damage;
            var targets = dealer.CombatState.Creatures.Where(c => c.IsEnemy);

            await CreatureCmd.Damage(choiceContext, targets, damage.BaseValue, damage.Props, dealer, null);
        }
        catch (Exception ex)
        {
            // 记录日志，避免静默失败
            GD.PrintErr($"ChildhoodBag patch error: {ex}");
        }
    }
}

/// <summary>
/// 童忆书包：
/// 用污浊药水（FOUL_POTION）填满你的空药水栏位。
/// 将战斗结束时的金币奖励替换为1瓶污浊药水（不再掉落金币奖励，并掉落1瓶污浊药水）。
/// 自身免疫污浊药水造成的伤害。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class ChildhoodBag : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("PotionCount", 1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPotion<FoulPotion>()];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        // 用幸运补剂填满所有空药水栏位
        for (int i = 0; i < base.Owner.MaxPotionCount; i++)
        {
            if (base.Owner.GetPotionAtSlotIndex(i) == null)
            {
                await PotionCmd.TryToProcure<FoulPotion>(base.Owner);
            }
        }
    }

    /// <summary>
    /// 战斗结束时，将金币奖励替换为1瓶污浊药水。
    /// </summary>
    public override bool TryModifyRewards(Player player, System.Collections.Generic.List<Reward> rewards,
        AbstractRoom? room)
    {
        if (player != base.Owner) return false;
        if (room is not CombatRoom) return false;

        var modified = false;
        for (var i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is GoldReward)
            {
                // 替换金币奖励为污浊药水
                rewards[i] = new PotionReward(ModelDb.Potion<FoulPotion>().ToMutable(), player);
                modified = true;
                break;
            }
        }

        // 如果没有金币奖励，也额外添加一瓶污浊药水
        if (!modified)
        {
            rewards.Add(new PotionReward(ModelDb.Potion<FoulPotion>().ToMutable(), player));
            modified = true;
        }

        return modified;
    }

    /// <summary>
    /// 免疫污浊药水造成的伤害。
    /// TODO: 当前 STS2 API 暂无明确标记药水伤害来源的机制，需要后续补充精确检测。
    /// </summary>
    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner.Creature) return;
        // TODO: 补充污浊药水伤害检测逻辑
    }
}