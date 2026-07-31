using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.encounters;

namespace TouhouAncients.Scripts;

public abstract class TouhouAncientBase : CustomAncientModel
{
    /// <summary>挑战胜利后奖励的金币数量。</summary>
    private const int ChallengeGoldReward = 150;

    /// <summary>挑战胜利后恢复已损失生命值的比例。</summary>
    private const decimal ChallengeHealRatio = 0.5m;

    public abstract int? ShowAct { get; }

    /// <summary>
    /// 可选：挑战战斗的 Encounter。返回非 null 时，Ancient 事件的选项列表末尾会追加一个
    /// "挑战"选项（参照 PUNCH_OFF 的本地化与选项实现），点击后进入该 Encounter 的战斗。
    /// 胜利后奖励为本次 Ancient 选项界面上的全部遗物、150 金币，并恢复已损失生命值的 50%。
    /// 默认返回 null（不启用挑战）。启用时请在子类中返回对应的 Encounter。
    /// </summary>
    public virtual YorigamiSistersEncounter? ChallengeEncounter => null;

    /// <summary>
    /// 进入战斗要求事件为共享事件（BaseLib 要求：Required for combat events）。
    /// 仅当启用挑战时共享，避免影响其他 Ancient 的多人行为。
    /// </summary>
    public override bool IsShared => ChallengeEncounter != null;

    /// <summary>最近一次生成的常规遗物选项（不含挑战选项），用于挑战胜利后的遗物奖励。</summary>
    private IReadOnlyList<EventOption>? _generatedRelicOptions;

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
        var options = base.GenerateInitialOptions().ToList();
        _generatedRelicOptions = options;
        if (ChallengeEncounter != null)
        {
            options.Add(CreateChallengeOption());
        }
        return options;
    }

    /// <summary>
    /// 创建"挑战"选项。本地化键：{AncientId}.fight.title / .fight.description
    /// （位于 ancients.json）。注意 textKey 只传前缀，EventOption 构造函数会
    /// 自动追加 ".title" / ".description" 后缀。
    /// </summary>
    protected EventOption CreateChallengeOption()
    {
        return new EventOption(this, StartChallenge, $"{Id.Entry}.fight");
    }

    /// <summary>
    /// 点击"挑战"：进入挑战战斗（不生成正常战斗奖励），战斗结束后恢复本事件
    /// （shouldResumeAfterCombat: true），在 <see cref="Resume"/> 中结算奖励（全部遗物 +
    /// 150 金币）、恢复已损失生命值的 50% 并结束事件。
    /// </summary>
    private Task StartChallenge()
    {
        // 标记为挑战战斗：ShouldGiveRewards = false，不生成任何正常战斗奖励。
        // 奖励全部由 Resume 在胜利后自行结算（参照 BattlewornDummy 的事件战斗模式）。
        var encounter = (YorigamiSistersEncounter)ChallengeEncounter!.ToMutable();
        encounter.IsChallenge = true;
        EnterCombatWithoutExitingEvent(encounter, Array.Empty<Reward>(), shouldResumeAfterCombat: true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 挑战战斗胜利后恢复本事件。注意必须先 SetEventFinished 再发奖励（对齐 BattlewornDummy）：
    /// EventRoom.Resume 中 ResumeEvents 是 fire-and-forget 调用本方法，随后会同步重建事件界面
    /// （NEventRoom.Create）。若本方法第一个操作是 await（让出控制权），界面重建时事件仍处于
    /// 战斗前的对话/选项状态，而战斗结束后事件场景资源（ancient_event_layout.tscn、
    /// ancient_dialogue_line.tscn）不在 PreloadManager 缓存中，Ancient 布局的对话容器为空，
    /// 玩家点击对话气泡会导致 NAncientEventLayout.SetDialogueLineAndAnimate 的 GetChild(0) 越界崩溃。
    /// 因此先把 SetEventFinished（同步、无前置 await）放在最前面，使界面重建时直接显示结束态
    /// （无对话、无选项），随后再治疗、发奖励。
    /// </summary>
    public override async Task Resume(AbstractRoom room)
    {
        // 同步最先执行：结束事件（无前置 await，确保 NEventRoom.Create 前事件已 finished）。
        SetEventFinished(L10NLookup($"{Id.Entry}.fight.done"));

        // 恢复已损失生命值的 50%。
        if (LocalContext.IsMe(Owner))
        {
            foreach (var player in Owner!.RunState.Players)
            {
                var lostHp = player.Creature.MaxHp - player.Creature.CurrentHp;
                await CreatureCmd.Heal(player.Creature, lostHp * ChallengeHealRatio);
            }
        }

        // 发放挑战奖励：全部遗物 + 150 金币（奖励界面由 OfferCustom 弹出，只含给定奖励）。
        var relicModels = _generatedRelicOptions?.Select(o => o.Relic).OfType<RelicModel>() ?? Array.Empty<RelicModel>();
        var rewards = new List<Reward> { new GoldReward(ChallengeGoldReward, Owner!) };
        foreach (var relic in relicModels)
        {
            // 每个玩家使用独立的遗物实例（从 canonical 克隆），避免多个奖励共享同一个 mutable 实例。
            var canonicalRelic = relic.CanonicalInstance ?? relic;
            rewards.Add(new RelicReward(canonicalRelic.ToMutable(), Owner!));
        }
        await RewardsCmd.OfferCustom(Owner!, rewards);
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