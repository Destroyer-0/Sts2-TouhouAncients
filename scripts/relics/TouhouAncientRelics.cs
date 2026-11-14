using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.relics;
using TouhouAncients.Scripts.relics.Akyuu;

//namespace TouhouAncients.Scripts.relics;

public abstract class TouhouAncientRelics : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public virtual string DefaultFileName => "default";

    /// <summary>极速模式不出现。金币/商店/地图分叉在极速里均不存在，这类遗物整条效果归零。</summary>
    protected virtual bool BlockedInBlitz => false;

    /// <summary>本模组专属的出现条件（牌组/生命/幕数等），只管自己这一层。
    /// 通用规则（模式黑名单）不写在这，重写时也就不必记得调 base。</summary>
    public virtual bool CanAppear(Player? player) => true;

    /// <summary>抽签统一入口：本模组条件 + 原版通用规则（模式/三幕黑名单）。
    /// 只调 CanAppear 会漏掉原版黑名单，只调 IsAllowed 会漏掉本模组条件。</summary>
    public bool CanBeOffered(Player? player)
        // 无 player（图鉴/预生成）时只能信 CanAppear
        => CanAppear(player) && (player == null || IsAllowed(player.RunState));

    // 抽遗物奖励与选项池是两条独立路径；极速拦截放这里，CanAppear 被 override 也不会丢
    protected override bool IsAllowedInternal(IRunState runState)
        => !(BlockedInBlitz && runState.GameMode == GameMode.Blitz);

    /// <summary>蔷薇偶像遗物的通用悬停：气氛的说明挂在「嗨到爆」词条里，两个要给在一起。</summary>
    protected IEnumerable<IHoverTip> KoishiPerformanceHoverTips =>
    [
        HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientAtmosphere),
        HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientHyped),
    ];

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

    /// <summary>拾起手稿或小说集时，若另一件已在手，吞掉对方并把自身换成阿加莎·克里斯Q侦探集。</summary>
    protected async Task MergeDetectiveCollection()
    {
        Player? player = base.Owner;
        if (player == null) return;

        // 两件互斥：自身是手稿就找小说集，反之亦然
        RelicModel? other = this is DetectiveManuscript
            ? player.GetRelic<DetectiveStory>()
            : this is DetectiveStory
                ? player.GetRelic<DetectiveManuscript>()
                : null;
        if (other == null) return;

        // 先移除对方再替换自身，顺序反了会短暂留下两件合体遗物
        await RelicCmd.Remove(other);
        await RelicCmd.Replace(this, ModelDb.Relic<AgathaChrisQCollection>().ToMutable());
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