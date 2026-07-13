using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts;

public abstract class TouhouAncientBase : CustomAncientModel
{
    public abstract int? ShowAct { get; }

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