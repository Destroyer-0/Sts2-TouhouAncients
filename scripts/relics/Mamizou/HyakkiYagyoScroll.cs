using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.Rewards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 百鬼夜行绘卷：拾起时，将1张[gold]百鬼夜行[/gold]加入你的[gold]牌组[/gold]，
/// 然后[aqua]誊写[/aqua]来自铁甲战士、静默猎者、储君、亡灵契约师、故障机器人的各一组卡牌奖励。
/// 每当你[gold]斩杀[/gold]时，战斗奖励增加一次随机其他角色的[aqua]誊写[/aqua]。
/// </summary>
/// <remarks>
/// 誊写记录与战斗中的消耗都在百鬼夜行这张牌上，遗物只负责发放百鬼夜行与誊写奖励。
/// </remarks>
[Pool(typeof(EventRelicPool))]
public class HyakkiYagyoScroll : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardPacks", 1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<HyakkiYagyo>().Append(TranscribeTip);

    /// <summary>「誊写」的定义提示（遗物描述里以 [aqua]誊写[/aqua] 标注它）；誊写奖励的按钮也复用它。</summary>
    internal IHoverTip TranscribeTip =>
        new HoverTip(
            new LocString("relics", Id.Entry + ".transcribeTitle"),
            new LocString("relics", Id.Entry + ".transcribeDescription"));

    /// <summary>
    /// 拾起时固定誊写的五个角色；只有这一处固定角色，其余誊写走玩家已解锁的其他角色。
    /// </summary>
    private IEnumerable<CardPoolModel> PickupPools =>
    [
        ModelDb.Character<Ironclad>().CardPool,
        ModelDb.Character<Silent>().CardPool,
        ModelDb.Character<Regent>().CardPool,
        ModelDb.Character<Necrobinder>().CardPool,
        ModelDb.Character<Defect>().CardPool,
    ];

    /// <summary>
    /// 拾起时：先将1张百鬼夜行加入牌组，再誊写五个角色各一组卡牌奖励。
    /// </summary>
    public override async Task AfterObtained()
    {
        Player player = base.Owner;

        CardModel card = player.RunState.CreateCard<HyakkiYagyo>(player);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);

        await OfferTranscribe(player, PickupPools);
    }

    /// <summary>
    /// 每当你斩杀时：战斗奖励增加一次随机其他角色的誊写。
    /// 斩杀判定与原版 Feed / 本 mod 地狱猫车一致（排除爪牙与复活中的敌人）。
    /// </summary>
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (!creature.IsEnemy) return Task.CompletedTask;
        if (!creature.Powers.All(power => power.ShouldOwnerDeathTriggerFatal())) return Task.CompletedTask;

        Player player = base.Owner;
        // 牌组中没有百鬼夜行：不触发誊写奖励。
        if (!HasHyakkiYagyoInDeck(player)) return Task.CompletedTask;
        if (player.RunState.CurrentRoom is not CombatRoom combatRoom) return Task.CompletedTask;

        // 这里的誊写不固定角色：从玩家已解锁的角色里随机一个「其他角色」。
        List<CardPoolModel> candidates = player.UnlockState.CharacterCardPools
            .Where(pool => pool.Id != player.Character.CardPool.Id)
            .ToList();
        if (candidates.Count == 0) return Task.CompletedTask;

        CardPoolModel? pool = player.PlayerRng.Rewards.NextItem(candidates);
        if (pool == null) return Task.CompletedTask;

        Flash();
        combatRoom.AddExtraReward(player, new TranscribeReward(player) { Pool = pool });
        return Task.CompletedTask;
    }

    /// <summary>为玩家提供若干角色卡池的誊写奖励。</summary>
    private async Task OfferTranscribe(Player player, IEnumerable<CardPoolModel> pools)
    {
        List<Reward> rewards = pools
            .Select(pool => (Reward)new TranscribeReward(player) { Pool = pool })
            .ToList();
        if (rewards.Count == 0) return;

        await RewardsCmd.OfferCustom(player, rewards);
    }

    /// <summary>牌组中是否有百鬼夜行；没有则不触发誊写。</summary>
    private bool HasHyakkiYagyoInDeck(Player player) =>
        PileType.Deck.GetPile(player).Cards.Any(card => card is HyakkiYagyo);
}
