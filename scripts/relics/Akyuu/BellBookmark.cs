using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 铃铛书签：在第一幕，卡牌奖励额外增加一张无色牌，且？房间出现怪物的概率大幅提升。
///
/// 出怪率照原版致命事件 <c>DeadlyEvents</c>：<c>MonsterOdds</c>（当前值）与
/// <c>SetBaseOdds</c>（命中后的重置目标）都要设，否则 <c>Roll</c> 一命中就打回 0.1。
/// <c>_baseOdds</c> 不随存档序列化，所以每次进房间/进幕都补写一次（兜读档与换幕）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BellBookmark : TouhouAncientRelics
{
    /// <summary>? 房间出怪的目标权重：宝箱 2% + 商店 3% 共占 5%，故与事件各占 47.5%。</summary>
    private const float MonsterOddsTarget = 0.475f;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override bool HasUponPickupEffect => true;

    private bool InFirstAct => base.Owner != null && base.Owner.RunState.CurrentActIndex == 0;

    public override Task AfterObtained()
    {
        ApplyOdds();
        return Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        ApplyOdds();
        return Task.CompletedTask;
    }

    public override Task AfterActEntered()
    {
        ApplyOdds();
        return Task.CompletedTask;
    }

    /// <summary>未命中时按原版的 0.1 正常累加（不能直接用 oddsIncrease：base 已被改成 0.475）。</summary>
    public override float ModifyOddsIncreaseForUnrolledRoomType(RoomType roomType, float oddsIncrease)
    {
        if (roomType != RoomType.Monster) return oddsIncrease;
        if (!InFirstAct) return oddsIncrease;

        return UnknownMapPointOdds.baseMonsterOdds;
    }

    /// <summary>第一幕把当前值与重置目标都钉在目标值；之后一并收回 base，等于「只在第一幕」。</summary>
    private void ApplyOdds()
    {
        var odds = base.Owner?.RunState.Odds.UnknownMapPoint;
        if (odds == null) return;

        var value = InFirstAct ? MonsterOddsTarget : UnknownMapPointOdds.baseMonsterOdds;
        odds.SetBaseOdds(RoomType.Monster, value);
        odds.MonsterOdds = InFirstAct ? Math.Max(odds.MonsterOdds, value) : value;
    }

    /// <summary>卡牌奖励里追加一张无色牌。</summary>
    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewards,
        CardCreationOptions creationOptions)
    {
        if (player != base.Owner) return false;
        if (!InFirstAct) return false;
        if (creationOptions.Source != CardCreationSource.Encounter) return false;

        // 稀有度权重跟随原奖励房间：精英/首领分别用 Elite/Boss 权重（Boss 必出稀有）。
        // 源仍标记为 Other，避免推进本体的稀有度保底计数。
        var options = new CardCreationOptions(
                [ModelDb.CardPool<ColorlessCardPool>()],
                CardCreationSource.Other,
                creationOptions.RarityOdds)
            .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoRarityModification);
        var card = CardFactory.CreateForReward(player, 1, options).FirstOrDefault()?.Card;
        if (card == null) return false;

        var result = new CardCreationResult(card);
        result.ModifyCard(card, this);
        cardRewards.Add(result);
        Flash();
        return true;
    }
}
