using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts;
using TouhouAncients.Scripts.relics;

//namespace TouhouAncients.Scripts.relics;

public abstract class TouhouAncientRelics : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public virtual string DefaultFileName => "default";

    /// <summary>先古之民选项条件：返回 false 时本遗物不会被抽为选项。由 TouhouAncientBase 在抽取前统一判定。</summary>
    public virtual bool CanAppear(Player? player) => true;

    /// <summary>玩家在当前地图点最后选的休息处选项 ID，没选过则为 null。
    /// 供「选完某项不消耗其余选项」判定：RestSiteSynchronizer 在调 ShouldDisableRemainingRestSiteOptions 前刚写入该记录。</summary>
    protected string? LastRestSiteChoice(Player player)
    {
        var choices = player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).RestSiteChoices;
        return choices is { Count: > 0 } ? choices[^1] : null;
    }

    /// <summary>
    /// 注册卡牌奖励的替代选项。原版 <c>CardRewardAlternative.Generate</c> 最多只接受 2 个（跳过恒占 1 个），
    /// 超了会抛异常、奖励永远领不掉，所以后来者挤掉先来者（同类遗物也互相挤）。
    /// </summary>
    protected static void AddCardRewardAlternative(List<CardRewardAlternative> alternatives, CardRewardAlternative alternative)
    {
        if (alternatives.Count > 1) alternatives.RemoveAt(alternatives.Count - 1);
        alternatives.Add(alternative);
    }

    /// <summary>
    /// 叠加卡池过滤器。WithFilter 是覆盖式就地赋值，直接调会顶掉别的遗物（缸中之脑/防晒霜/大猎物猎人）已设的过滤。
    /// </summary>
    protected CardCreationOptions AndFilter(CardCreationOptions options, Func<CardModel, bool> filter)
    {
        // prev 必须在 WithFilter 之前取，它是就地改
        var prev = options.CardPoolFilter;
        return prev == null ? options.WithFilter(filter) : options.WithFilter(c => prev(c) && filter(c));
    }

    /// <summary>该牌是否被玩家的缸中之脑屏蔽。防晒霜要让路给它，避免两者叠加后奖励池为空。</summary>
    protected bool IsBlockedByVat(Player player, CardModel card)
    {
        return player.Relics.OfType<BrainInAVat>().Any(r => r.IsBlocked(card.Id.Entry));
    }

    // 小图标（原版85x85）
    public override string PackedIconPath => TouhouAncientCmd.CheckPathExistsWithFallback(
        $"res://images/icon/relics/{GetType().Name}.png",
        $"res://images/icon/relics/{DefaultFileName}.png");

    // 轮廓图标（原版85x85）
    protected override string PackedIconOutlinePath => TouhouAncientCmd.CheckPathExistsWithFallback(
        $"res://images/icon/relics/{GetType().Name}.png",
        $"res://images/icon/relics/{DefaultFileName}.png");

    // 大图标（原版256x256）
    protected override string BigIconPath => TouhouAncientCmd.CheckPathExistsWithFallback(
        $"res://images/icon/relics/IconLarge/{GetType().Name}.png",
        TouhouAncientCmd.CheckPathExistsWithFallback(
            $"res://images/icon/relics/{GetType().Name}.png",
            $"res://images/icon/relics/IconLarge/{DefaultFileName}.png"));
}