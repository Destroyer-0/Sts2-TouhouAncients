using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.encounters;

namespace TouhouAncients.Scripts;

public abstract class TouhouAncientBase : CustomAncientModel
{

    public abstract int? ShowAct { get; }

    /// <summary>
    /// 可选：挑战战斗的 Encounter。返回非 null 时，选项列表末尾追加"挑战"选项。
    /// 挑战战斗是终点（不返回事件页）：胜利后按标准战斗奖励流程弹出奖励页
    /// （奖励为本玩家随机池里的全部遗物），并恢复已损失生命的 50%（见 ChallengeRewards）。
    /// </summary>
    public virtual TouhouAncientEncounter? ChallengeEncounter => null;

    /// <summary>进入战斗要求事件为共享事件（BaseLib 要求），仅启用挑战时共享。</summary>
    public override bool IsShared => ChallengeEncounter != null;

    /// <summary>最近一次生成的遗物选项（不含挑战选项），用于挑战胜利后的遗物奖励。</summary>
    private IReadOnlyList<EventOption>? _generatedRelicOptions;

    /// <summary>
    /// 是否处于"按玩家分别结算"阶段（由 AncientMultiplayerVotePatch 置位）：此时没有随机抽取，
    /// 每个玩家拿到自己投的遗物，UI 据此跳过"随机选择"的投票动画与音效。
    /// </summary>
    public bool IsPerPlayerResolution { get; set; }

    /// <summary>本 Ancient 的全部选项（含挑战选项）。</summary>
    public override IEnumerable<EventOption> AllPossibleOptions => AllOptions;

    /// <summary>
    /// 本 Ancient 的选项：一行一个池，写几行就是几个选项；
    /// 写成 (池, N) 表示该池产出 N 个选项（池内不重复）。
    /// 条件过滤、同一事件内不重复与随机都由基类在抽取前统一处理。
    /// </summary>
    protected abstract IReadOnlyList<TARelicOptionGroup> TARelicOptionPools { get; }

    /// <summary>
    /// 可选：追加在遗物选项之后的额外（非遗物）选项。
    /// 与挑战选项一样只在生成阶段追加，不参与池的抽取与去重，也不会进入挑战奖励的遗物列表。
    /// 注意：BaseLib 兼容用的 <see cref="MakeOptionPools"/> 只能装遗物候选，额外选项不会出现在那里。
    /// </summary>
    protected virtual IEnumerable<EventOption> ExtraOptions => [];

    /// <summary>按遗物创建一个候选（weight 传 0 表示不出现）。</summary>
    protected TARelicOption TARelicOption<T>(int weight = 1) where T : RelicModel
        => new(RelicOption<T>()) { Weight = weight };

    /// <summary>创建一个候选池。</summary>
    protected TARelicOptionPool CreateTARelicOptionPool(params TARelicOption[] options)
        => new() { Options = options };

    /// <summary>挑战选项（启用挑战时才有，只有一个；不参与池的抽取与去重）。</summary>
    private IEnumerable<EventOption> ChallengeOptions =>
        ChallengeEncounter == null ? [] : [CreateChallengeOption()];

    /// <summary>遗物池的候选 + 额外选项 + 挑战选项，供图鉴 / 控制台 / 调试选项使用。</summary>
    private IEnumerable<EventOption> AllOptions =>
    [
        .. TARelicOptionPools.SelectMany(group => group.Pool.Options),
        .. ExtraOptions,
        .. ChallengeOptions
    ];

    // BaseLib 要求实现；本体系的抽签不走它，仅作为 BaseLib 自身逻辑的兼容来源。
    protected sealed override OptionPools MakeOptionPools => new OptionPools(MakePool(
        TARelicOptionPools.SelectMany(group => group.Pool.Options)
            .Select(candidate => candidate.Relic)
            .OfType<RelicModel>()
            .DistinctBy(relic => relic.Id)
            .Select(relic => (AncientOption)relic)
            .ToArray()));

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // 挑战战斗是共享事件，共享事件的事件级 Rng 不含玩家槽位（全员同池）；
        // 用 Rng(Player, Id) 派生独立 RNG，让多人各玩家看到各自的选项，且各端规则一致。
        var rng = ChallengeEncounter != null ? new Rng(Owner!, Id) : Rng;

        var options = new List<EventOption>();
        var taken = new HashSet<ModelId>();
        foreach (var (pool, count) in TARelicOptionPools)
        {
            // 过滤放在抽取之前：条件不满足、权重为 0 或已经给过的遗物不会占掉一个选项。
            var available = pool.Options
                .Where(candidate => candidate.Weight > 0)
                .Where(candidate => candidate.Relic is not RelicModel relic || !taken.Contains(relic.Id))
                .Where(candidate => candidate.Relic is not TouhouAncientRelics relic || relic.CanAppear(Owner))
                .ToList();

            for (var i = 0; i < count && available.Count > 0; i++)
            {
                var chosen = PickWeighted(available, rng);
                if (chosen.Relic is RelicModel picked) taken.Add(picked.Id);
                available.Remove(chosen);
                options.Add(chosen);
            }
        }

        // 遗物选项用于挑战奖励（上面的 options 会被追加额外选项与挑战选项，所以要单独保留一份）。
        _generatedRelicOptions = [.. options];
        options.AddRange(ExtraOptions);
        options.AddRange(ChallengeOptions);
        return options;
    }

    /// <summary>按权重随机取一个候选。</summary>
    private static TARelicOption PickWeighted(List<TARelicOption> candidates, Rng rng)
    {
        var roll = rng.NextInt(candidates.Sum(candidate => candidate.Weight));
        foreach (var candidate in candidates)
        {
            roll -= candidate.Weight;
            if (roll < 0) return candidate;
        }

        return candidates[^1];
    }

    /// <summary>多人模式下挑战选项的协同提示 HoverTip 本地化键（ancients 表）。</summary>
    private const string ChallengeCoopPromptKey = "TOUHOUANCIENTS-CHALLENGE_COOP";

    /// <summary>
    /// 创建"挑战"选项。标题优先匹配 {AncientId}.fight.{token}.title（token 取 Character.Id.Entry 的子串），
    /// 无匹配时回退 {AncientId}.fight.title；textKey 固定为 {AncientId}.fight，供挑战按钮图标识别，不要改。
    /// 多人时附加"合作事件"HoverTip。
    /// </summary>
    protected EventOption CreateChallengeOption()
    {
        var hoverTips = Owner != null && Owner.RunState.Players.Count > 1
            ? new IHoverTip[] { new HoverTip(new LocString("ancients", ChallengeCoopPromptKey)) }
            : Array.Empty<IHoverTip>();
        return new EventOption(
            this, 
            StartChallenge,
            GetChallengeTitleLoc(),
            new LocString("ancients","TOUHOUANCIENTS.fight.description"),
            $"{Id.Entry}.fight", 
            hoverTips);
    }

    /// <summary>
    /// 解析挑战选项标题：扫描 {AncientId}.fight.{token}.title，取 Character.Id.Entry 包含的 token（更长优先），
    /// 无匹配时用 {AncientId}.fight.title。以后加一条 fight.{token}.title 本地化即可扩展，不必改代码。
    /// </summary>
    protected virtual LocString GetChallengeTitleLoc()
    {
        return CharacterLocVariant.Find(
                   "ancients",
                   $"{Id.Entry}.fight.",
                   ".title",
                   Owner?.Character.Id.Entry)
               ?? new LocString("ancients", $"{Id.Entry}.fight.title");
    }

    /// <summary>
    /// 点击"挑战"：进入挑战战斗（shouldResumeAfterCombat: false，胜利后不返回事件页，
    /// 由标准战斗奖励流程在战斗结束时弹出奖励页）。
    ///
    /// IsChallenge 采用"置位-复位"：在 canonical 上置位后传入（v0.110.1 起要求传 canonical，
    /// 游戏内部同步克隆出战斗实例并继承该标志），调用返回后立即复位，避免后续非挑战战斗被误判。
    ///
    /// 奖励只收集本实例所属玩家（base.Owner）的池，交给游戏合并进共享战斗；
    /// 若在此遍历 EventSynchronizer.Events 收集全员，N 个实例会各合并一份"全员合集"，奖励变成人数倍。
    /// </summary>
    private Task StartChallenge()
    {
        var encounter = ChallengeEncounter!;
        var ancientEncounter = (TouhouAncientEncounter)encounter;
        ancientEncounter.IsChallenge = true; // 置位：游戏克隆战斗实例时（MemberwiseClone）会继承该标志
        try
        {
            // 只收集本玩家（base.Owner）自己随机池的遗物，作为 extraRewards 存入
            // CombatRoom.ExtraRewards（各玩家实例各自收集，由游戏合并）。
            // 每个奖励用独立的 mutable 实例（从 canonical 克隆），避免多个奖励共享同一个实例。
            var rewards = new List<Reward>();
            var relicModels = _generatedRelicOptions?.Select(o => o.Relic).OfType<RelicModel>()
                              ?? Array.Empty<RelicModel>();
            foreach (var relic in relicModels)
            {
                var canonicalRelic = relic.CanonicalInstance ?? relic;
                rewards.Add(new RelicReward(canonicalRelic.ToMutable(), Owner!));
            }
            // 传入 canonical（v0.110.1 起要求），游戏内部克隆出的战斗实例会继承 IsChallenge。
            EnterCombatWithoutExitingEvent(encounter, rewards, shouldResumeAfterCombat: false);
        }
        finally
        {
            // 战斗实例已克隆完成，复位 canonical 标志，避免后续非挑战战斗被误判。
            ancientEncounter.IsChallenge = false;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 可选：多说话者配置（键为 loc 后缀标识符，如 "jyoon"）。返回 null 表示单一说话者，
    /// 使用默认 RunHistoryIcon 与 DialogueColor。
    /// </summary>
    public virtual IReadOnlyDictionary<string, AncientSpeakerProfile>? SpeakerProfiles => null;

    /// <summary>根据选项索引（对应选项槽位）返回按钮颜色，默认用 ButtonColor。</summary>
    public virtual Color GetOptionButtonColor(int optionIndex) => ButtonColor;

    /// <summary>
    /// 第一幕先古之民的「涅奥式」入场：原版只在 <c>this is Neow</c> 时做两步——
    /// 进入事件前清零生命（让回满算出的 HealedAmount 是满血回复量），事件开始后播放顶栏生命条补间。
    /// 本 Mod 的 <see cref="ShowAct"/> == 1 复刻这两步（用 override 而非 Transpiler：该方法是 async，改 IL 脆弱）。
    /// </summary>
    protected override async Task BeforeEventStarted(bool isPreFinished)
    {
        bool isAct1Ancient = ShowAct == 1;

        // 与涅奥一致：非读档恢复时才清零，读档恢复不重置生命。
        if (isAct1Ancient && !isPreFinished)
        {
            Owner!.Creature.SetCurrentHpInternal(0m);
        }

        await base.BeforeEventStarted(isPreFinished);

        // base 已完成回满并写入 HealedAmount，此时播放顶栏生命条补间（与涅奥时机一致）。
        // 与涅奥一样是"发起后不等待"的演出，用弃元显式忽略返回的 Task；await 会阻塞事件初始化。
        if (isAct1Ancient && !isPreFinished && NRun.Instance != null)
        {
            _ = TaskHelper.RunSafely(NRun.Instance.GlobalUi.TopBar.Hp.LerpAtNeow());
        }
    }

    public override bool IsValidForAct(ActModel act)
    {
        if (TouhouAncientsConfig.IsAncientBanned(this))
        {
            return false;
        }
        if (!ShowAct.HasValue)
        {
            return base.IsValidForAct(act);
        }
        
        return act.ActNumber() == ShowAct.Value;
    }
    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced(this, act.ActNumber());
    }
}

/// <summary>
/// 一个先古之民选项候选：就是选项本身，外加权重（默认 1，传 0 表示不出现）。
/// 注意：红闪等标记必须在包装之后链式添加 —— <see cref="EventOption"/> 的拷贝构造函数不复制
/// <c>WillKillPlayer</c> / <c>ShouldSaveChoiceToHistory</c>，先打标记再包装会丢失。
/// </summary>
public sealed class TARelicOption : EventOption
{
    public TARelicOption(EventOption option) : base(option)
    {
        // 拷贝构造不复制 Relic，这里补回来（条件过滤、去重与图鉴都依赖它）。
        if (option.Relic is { } relic)
        {
            WithRelic(relic);
        }
    }

    /// <summary>被抽中的相对权重（默认 1；0 表示不出现）。</summary>
    public int Weight { get; init; } = 1;

    // 原版这些方法不是 virtual 且返回 EventOption，这里重声明以保持链式的静态类型不退化（内部返回的就是自身）。

    /// <summary>玩家的当前生命不高于该值时，选项闪红。</summary>
    public new TARelicOption ThatDoesDamage(decimal damage) => (TARelicOption)base.ThatDoesDamage(damage);

    /// <summary>玩家的最大生命不高于该值时，选项闪红。</summary>
    public new TARelicOption ThatDecreasesMaxHp(decimal value) => (TARelicOption)base.ThatDecreasesMaxHp(value);

    /// <summary>传入的判定为 true 时，选项闪红。</summary>
    public new TARelicOption ThatWillKillPlayerIf(Func<Player, bool> willKillPlayer) =>
        (TARelicOption)base.ThatWillKillPlayerIf(willKillPlayer);

    /// <summary>把标题的本地化变量写入存档。</summary>
    public new TARelicOption ThatHasDynamicTitle() => (TARelicOption)base.ThatHasDynamicTitle();

    /// <summary>不把该选项记入选择历史。</summary>
    public new TARelicOption ThatWontSaveToChoiceHistory() => (TARelicOption)base.ThatWontSaveToChoiceHistory();

    /// <summary>覆盖该选项在历史记录里显示的名称。</summary>
    public new TARelicOption WithOverridenHistoryName(LocString historyName) =>
        (TARelicOption)base.WithOverridenHistoryName(historyName);

    /// <summary>
    /// 在生成选项之前对该候选的遗物做一次性准备（等价于 BaseLib 的 ModelPrep），
    /// 例如按当前角色填好描述里的动态变量。此阶段遗物自身的 Owner 尚未设置，
    /// 需要用古代事件自己的 Owner。
    /// </summary>
    public TARelicOption Prep<TModel>(Action<TModel> prepare) where TModel : RelicModel
    {
        if (Relic is TModel relic)
        {
            prepare(relic);
        }

        return this;
    }
}

/// <summary>一个候选池：池内候选互斥，同一次事件里不会被抽中两次。</summary>
public sealed class TARelicOptionPool
{
    public required IReadOnlyList<TARelicOption> Options { get; init; }
}

/// <summary>
/// 「某个池产出 N 个选项」的写法载体：直接写池时等价于 Count = 1，
/// 写成 (池, N) 时表示该池产出 N 个选项（池内不重复）。
/// </summary>
public readonly record struct TARelicOptionGroup(TARelicOptionPool Pool, int Count = 1)
{
    public static implicit operator TARelicOptionGroup(TARelicOptionPool pool) => new(pool);

    public static implicit operator TARelicOptionGroup((TARelicOptionPool Pool, int Count) pair) =>
        new(pair.Pool, pair.Count);
}