using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 「幸运」戏法：因幡帝塞进玩家手牌的 0 费状态牌，保留 + 消耗。
/// 打出后本回合随机化手牌耗能（其他「幸运」戏法除外），
/// 并略微偏向抽中诈术师当前的目标费用——凑不出该费用的攻击牌伤害会被减半。
/// </summary>
[Pool(typeof(StatusCardPool))]
public class LuckyTrick : TouhouAncientCards
{
    /// <summary>耗能取值范围 0~3。</summary>
    private const int CostValueCount = 4;

    /// <summary>目标费用的权重，其余 3 个值各占 1（命中率 2/5）。</summary>
    private const int TargetCostWeight = 2;

    private const int energyCost = 0;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Status;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override bool UseAncientFrame => true;

    /// <summary>常态立绘（绘师 すったく）：卡面恒定的底图。</summary>
    private const string DefaultPortraitPath = "res://images/cards/LuckyTrick1.png";

    /// <summary>打出张数变多后的立绘（绘师 ryosios）：叠在底图上随计数淡入。</summary>
    private const string AltPortraitPath = "res://images/cards/LuckyTrick2.png";

    // 过渡张数：打出 8 张时上层立绘完全盖住底图（原先是在第 3 张硬切）。
    private const int FadeSteps = 8;

    /// <summary>每打出这么多张，卡名的问号多一个。</summary>
    private const int PlaysPerQuestionMark = 2;

    /// <summary>问号上限，防止卡名越打越长。</summary>
    private const int MaxQuestionMarks = 4;

    // canonical 实例（图鉴、ModelDb 预热枚举 AllPortraitPaths）读 Owner 会 AssertMutable 抛异常，必须先判 IsMutable。
    // 每次刷新都会重新读：打出计数可以中途超过阈值，不能只在某个时机算一次
    private int PlayCount => IsMutable && Owner != null
        ? Owner.Creature.GetPowerAmount<LuckyTrickCountPower>()
        : 0;

    /// <summary>上层立绘的不透明度：随计数线性上升，到 <see cref="FadeSteps"/> 张时完全盖住底图。</summary>
    private float OverlayAlpha => Math.Clamp(PlayCount / (float)FadeSteps, 0f, 1f);

    /// <summary>卡节点上两张立绘叠着放：luckytrick1 恒铺底，luckytrick2 按计数淡入（不再是硬换图）。</summary>
    public override PortraitFade? PortraitFade => new(DefaultPortraitPath, AltPortraitPath, OverlayAlpha);

    /// <summary>过渡过半后（含绘师）都算备用那张的地盘；图鉴走 canonical 实例，计数为 0 仍是默认立绘。</summary>
    private bool UseAltPortrait => OverlayAlpha > 0.5f;

    /// <summary>BaseLib 的自定义立绘入口：走它才会被当成卡图加载（远古框会去 %AncientPortrait 那条分支）。</summary>
    public override string? CustomPortraitPath => UseAltPortrait ? AltPortraitPath : DefaultPortraitPath;

    public override string PortraitPath => UseAltPortrait ? AltPortraitPath : DefaultPortraitPath;

    /// <summary>两张都要列出，供 ModelDb 预热。</summary>
    public override IEnumerable<string> AllPortraitPaths => [DefaultPortraitPath, AltPortraitPath];

    public override string? Author => UseAltPortrait ? "ryosios" : "すったく";

    public override bool CanBeGeneratedByModifiers => false;

    public override bool CanBeGeneratedInCombat => false;

    public override int MaxUpgradeLevel => -1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    public LuckyTrick() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    /// <summary>每打出两张加一个问号（上限见 <see cref="MaxQuestionMarks"/>）。</summary>
    public override string Title
    {
        get
        {
            int marks = Math.Min(PlayCount / PlaysPerQuestionMark, MaxQuestionMarks);
            return marks == 0 ? base.Title : $"{base.Title}{new string('?', marks)}";
        }
    }
    // 层数变化本来就经 SetAmount → Creature.PowerIncreased → tracker.OnPowerIncreased 广播了一次
    // CombatStateChanged（下一帧发），手牌会跟着 UpdateVisuals → UpdatePortrait 重读立绘，先不额外播报。
    //
    // /// <summary>
    // /// 打出计数变化时重读立绘。钩子挂在卡自己身上：卡只在战斗牌堆里才收得到（<c>CardModel.ShouldReceiveCombatHooks</c>=<c>Pile?.IsCombatPile</c>），
    // /// 正好是本场战斗内生效。
    // /// </summary>
    // public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
    //     Creature? applier, CardModel? cardSource)
    // {
    //     if (power is LuckyTrickCountPower && power.Owner == Owner?.Creature)
    //     {
    //         NotifyCombatStateChanged();
    //     }
    //
    //     return Task.CompletedTask;
    // }
    //
    // /// <summary>原版私有的 <c>CombatStateTracker.NotifyCombatStateChanged</c>：和加牌/加 Power 时走的是同一步。</summary>
    // private static readonly MethodInfo? NotifyCombatStateChangedMethod =
    //     AccessTools.Method(typeof(CombatStateTracker), "NotifyCombatStateChanged");
    //
    // private static void NotifyCombatStateChanged()
    // {
    //     if (CombatManager.Instance?.StateTracker is { } tracker)
    //     {
    //         NotifyCombatStateChangedMethod?.Invoke(tracker, ["TouhouAncients.LuckyTrick"]);
    //     }
    // }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = base.Owner;
        if (player?.PlayerCombatState == null) return ;

        int? targetCost = FindTargetCost(player);
        foreach (CardModel card in player.PlayerCombatState.Hand.Cards)
        {
            // 其他「幸运」戏法保持 0 费，不参与随机化
            if (card is LuckyTrick) continue;
            if (card.EnergyCost.Canonical < 0) continue;

            card.EnergyCost.SetThisTurn(RollCost(player, targetCost));
        }

        await PowerCmd.Apply<LuckyTrickCountPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    /// <summary>当前诈术师的目标费用；场上没有诈术师时返回 null（退化为均等随机）。</summary>
    private static int? FindTargetCost(Player player)
    {
        if (player.Creature.CombatState is not { } combatState) return null;

        foreach (Creature enemy in combatState.Enemies)
        {
            if (enemy.GetPower<TricksterPower>() is { } trickster) return trickster.Amount;
        }

        return null;
    }

    private static int RollCost(Player player, int? targetCost)
    {
        if (targetCost is not { } target)
        {
            return player.RunState.Rng.CombatEnergyCosts.NextInt(CostValueCount);
        }

        int roll = player.RunState.Rng.CombatEnergyCosts.NextInt(CostValueCount + TargetCostWeight - 1);
        // 目标费用以外的 3 个值按 (target + 1 + offset) % 4 展开，天然不会落回目标值
        return roll < TargetCostWeight ? target : (target + 1 + roll - TargetCostWeight) % CostValueCount;
    }
}
