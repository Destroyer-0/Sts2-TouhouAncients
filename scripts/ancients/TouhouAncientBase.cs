using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.encounters;

namespace TouhouAncients.Scripts;

public abstract class TouhouAncientBase : CustomAncientModel
{

    public abstract int? ShowAct { get; }

    /// <summary>
    /// 可选：挑战战斗的 Encounter。返回非 null 时，Ancient 事件的选项列表末尾会追加一个
    /// "挑战"选项（参照 PUNCH_OFF 的本地化与选项实现），点击后进入该 Encounter 的战斗。
    /// 挑战战斗是终点（不返回事件页，参照 PUNCH_OFF / DenseVegetation）：胜利后走标准战斗
    /// 奖励流程在战斗结束时弹出奖励页（奖励为该玩家随机池里的全部遗物），并恢复已损失生命值
    /// 的 50%（由 ChallengeRewards 回调处理）。
    /// 默认返回 null（不启用挑战）。启用时请在子类中返回对应的 Encounter。
    /// </summary>
    public virtual TouhouAncientEncounter? ChallengeEncounter => null;

    /// <summary>
    /// 进入战斗要求事件为共享事件（BaseLib 要求：Required for combat events）。
    /// 仅当启用挑战时共享，避免影响其他 Ancient 的多人行为。
    /// </summary>
    public override bool IsShared => ChallengeEncounter != null;

    /// <summary>最近一次生成的常规遗物选项（不含挑战选项），用于挑战胜利后的遗物奖励。</summary>
    private IReadOnlyList<EventOption>? _generatedRelicOptions;

    /// <summary>
    /// 是否处于"按玩家分别结算"阶段（由 <c>AncientMultiplayerVotePatch</c> 在无人投挑战、
    /// 按玩家各自结算遗物时对所有事件实例置位）。UI 据此跳过"随机选择"的投票动画与音效
    /// （<c>EventSplitVoteAnimation</c> 的 map_split_tick.mp3）——因为此时没有随机抽取，
    /// 每个玩家拿到的是自己投的遗物。
    /// </summary>
    public bool IsPerPlayerResolution { get; set; }

    /// <summary>
    /// 本 Ancient 的全部选项（含挑战选项）。启用挑战时，挑战选项会追加在末尾。
    /// </summary>
    public override IEnumerable<EventOption> AllPossibleOptions
    {
        get
        {
            var options = GetAncientOptions().ToList();
            if (ChallengeEncounter != null)
            {
                options.Add(CreateChallengeOption());
            }
            return options;
        }
    }

    /// <summary>
    /// 获取本 Ancient 的常规选项（不含挑战选项）。默认返回遗物池生成的选项。
    /// 自定义选项列表的子类应重写此方法（而非 <see cref="AllPossibleOptions"/>），
    /// 以保证挑战选项始终被追加。
    /// </summary>
    protected virtual IEnumerable<EventOption> GetAncientOptions() => base.AllPossibleOptions;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // 每个玩家独立抽取遗物池：共享事件的事件级 Rng 不含玩家槽位（全员同池），
        // 这里用 Rng(Player, Id) 按"运行种子 + 玩家槽位 + 事件ID"派生独立 RNG
        // （对齐非共享事件的种子规则），保证多人时各玩家看到/掉落各自的随机遗物，
        // 且所有客户端按同一规则生成（跨端一致、可同步），单人行为与原逻辑一致。

        if (ChallengeEncounter != null)
        {
            var optionRng = new Rng(Owner!, Id);
            var options = OptionPools.Roll(optionRng, this)
                .Select(option => RelicOption(option.ModelForOption))
                .ToList();
            _generatedRelicOptions = options;
            options.Add(CreateChallengeOption());
            return options;
        }
        else
        {
            var options = base.GenerateInitialOptions().ToList();
            _generatedRelicOptions = options;
            return options;
        }
    }

    /// <summary>多人模式下挑战选项的协同提示 HoverTip 本地化键（ancients 表）。</summary>
    private const string ChallengeCoopPromptKey = "TOUHOUANCIENTS-CHALLENGE_COOP";

    /// <summary>
    /// 创建"挑战"选项。本地化键：{AncientId}.fight.title / .fight.description
    /// （位于 ancients.json）。注意 textKey 只传前缀，EventOption 构造函数会
    /// 自动追加 ".title" / ".description" 后缀。
    /// 仅多人模式给挑战选项附加"合作事件"HoverTip（悬停选项时显示），提示所有玩家
    /// 均确认进行挑战后方可开始；单人模式不附加。
    /// </summary>
    protected EventOption CreateChallengeOption()
    {
        var hoverTips = Owner != null && Owner.RunState.Players.Count > 1
            ? new IHoverTip[] { new HoverTip(new LocString("ancients", ChallengeCoopPromptKey)) }
            : Array.Empty<IHoverTip>();
        return new EventOption(
            this, 
            StartChallenge,
            new LocString("ancients",$"{Id.Entry}.fight.title"),
            new LocString("ancients","TOUHOUANCIENTS.fight.description"),
            $"{Id.Entry}.fight", 
            hoverTips);
    }

    /// <summary>
    /// 点击"挑战"：进入挑战战斗（不生成默认战斗奖励）。挑战战斗是终点（shouldResumeAfterCombat:
    /// false，参照 PUNCH_OFF / DenseVegetation）：胜利后走标准战斗奖励流程，在战斗结束时弹出
    /// 奖励页，不再返回事件页；读档后由 StartPreFinishedCombat → OfferRoomEndRewards 可靠地
    /// 重新弹出奖励页，不会丢失。
    /// </summary>
    private Task StartChallenge()
    {
        var encounter = ChallengeEncounter!.ToMutable();
        ((TouhouAncientEncounter)encounter).IsChallenge = true;

        // 为所有玩家收集各自随机池的遗物奖励，作为 extraRewards 存入 CombatRoom.ExtraRewards。
        // 各端所有事件实例的 _generatedRelicOptions 一致（每玩家独立 RNG、跨端可同步），
        // 因此各端 ExtraRewards 内容一致，多人奖励同步正常；该字段随存档持久化。
        var rewards = new List<Reward>();
        foreach (var eventModel in RunManager.Instance.EventSynchronizer.Events)
        {
            if (eventModel is not TouhouAncientBase ancient || ancient.Owner == null) continue;
            var relicModels = ancient._generatedRelicOptions?.Select(o => o.Relic).OfType<RelicModel>()
                              ?? Array.Empty<RelicModel>();
            foreach (var relic in relicModels)
            {
                // 每个玩家使用独立的遗物实例（从 canonical 克隆），避免多个奖励共享同一个 mutable 实例。
                var canonicalRelic = relic.CanonicalInstance ?? relic;
                rewards.Add(new RelicReward(canonicalRelic.ToMutable(), ancient.Owner));
            }
        }
        EnterCombatWithoutExitingEvent(encounter, rewards, shouldResumeAfterCombat: false);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 可选：多说话者配置。键为 loc 后缀标识符（如 "jyoon", "shion"），
    /// 值为对应的头像、outline 和对话气泡颜色。
    /// 返回 null 表示只有单一说话者，使用默认 RunHistoryIcon 和 DialogueColor。
    /// </summary>
    public virtual IReadOnlyDictionary<string, AncientSpeakerProfile>? SpeakerProfiles => null;

    /// <summary>
    /// 根据选项索引（对应 MakeOptionPools 中的第 N 个 MakePool）返回按钮颜色。
    /// 默认返回 <see cref="ButtonColor"/>。
    /// </summary>
    public virtual Color GetOptionButtonColor(int optionIndex) => ButtonColor;

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