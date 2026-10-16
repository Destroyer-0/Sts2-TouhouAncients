using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 诅咒之血：拾起时，获得一张腐朽。在你的每个回合开始时，炼制两瓶药水并喝下。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class CursedBlood : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PotionCount", 2), new CardsVar(2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Decay>();

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        await CardPileCmd.AddCursesToDeck(Enumerable.Repeat(ModelDb.Card<Decay>(),2),base.Owner);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        var rng = player.RunState.Rng.CombatPotionGeneration;

        // 获取所有可解锁的自指向药水
        var selfTargetPotions = PotionFactory.GetPotionOptions(player)
            .Where(p => p is { CanBeGeneratedInCombat: true, TargetType: TargetType.Self or TargetType.AnyPlayer })
            .ToList();

        if (selfTargetPotions.Count == 0) return;

        int count = DynamicVars["PotionCount"].IntValue;
        for (int i = 0; i < count; i++)
        {
            var selected = selfTargetPotions[rng.NextInt(selfTargetPotions.Count)].ToMutable();
            selected.Owner = player;

            Flash();
            await DrinkPotion(choiceContext, selected, player);

            // 等待药水效果完成
            await Cmd.Wait(0.5f);
        }
    }

    /// <summary>
    /// 直接喝下药水，不进药水栏。
    /// 不能走 OnUseWrapper：其首步 RemoveBeforeUse 会因找不到槽位抛异常，故反射调 OnUse 并自行补喝药水的钩子。
    /// 副作用是不再受「无法获得药水」（如 Sozu）限制，药水栏满时也能喝。
    /// </summary>
    protected async Task DrinkPotion(PlayerChoiceContext choiceContext, PotionModel potion, Player player)
    {
        var combatState = player.Creature.CombatState;
        var onUse = AccessTools.Method(typeof(PotionModel), "OnUse", [typeof(PlayerChoiceContext), typeof(Creature)]);
        if (onUse == null) return;

        await Hook.BeforePotionUsed(player.RunState, combatState, potion, player.Creature);

        // 补上 OnUseWrapper 里的投掷动画，否则喝药水毫无视觉反馈
        if (TestMode.IsOff)
        {
            var room = NCombatRoom.Instance;
            var node = room?.GetCreatureNode(player.Creature);
            if (room != null && node != null)
            {
                room.CombatVfxContainer.AddChildSafely(
                    NItemThrowVfx.Create(node.VfxSpawnPosition, node.GetBottomOfHitbox(), potion.Image));
                await Cmd.Wait(0.5f);
            }
        }

        choiceContext.PushModel(potion);
        var effectId = CombatManager.Instance.BeginCardOrPotionEffect(player);
        try
        {
            if (onUse.Invoke(potion, [choiceContext, player.Creature]) is Task task) await task;
        }
        finally
        {
            await CombatManager.Instance.EndCardOrPotionEffect(effectId, player);
            choiceContext.PopModel(potion);
        }

        potion.InvokeExecutionFinished();
        await Hook.AfterPotionUsed(player.RunState, combatState, potion, player.Creature);
    }
}
