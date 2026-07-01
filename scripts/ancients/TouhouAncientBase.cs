using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace TouhouAncients.Scripts;

public abstract class TouhouAncientBase : ModAncientEventTemplate
{
    /// <summary>
    /// ShowAct 是 1-based（如 2 = 第二幕）。
    /// null = 不限制幕数。
    /// </summary>
    public abstract int? ShowAct { get; }

    public override bool IsAllowed(IRunState runState)
    {
        if (TouhouAncientsConfig.IsAncientBanned(this))
            return false;
        if (!ShowAct.HasValue)
            return base.IsAllowed(runState);
        return runState.CurrentActIndex + 1 == ShowAct.Value;
    }

    /// <summary>
    /// 默认初始选项：展示所有 AllPossibleOptions。
    /// 子类如需自定义池逻辑可重写此方法。
    /// </summary>
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return AllPossibleOptions.ToList();
    }

    /// <summary>
    /// 检查此 Ancient 是否应当在当前幕被强制出现。
    /// 由 BanAncientPatch 的 transpiler 在生成房间时调用。
    /// </summary>
    public bool ShouldForceSpawn(int actNumber)
    {
        return TouhouAncientsConfig.IsAncientForced(this, actNumber);
    }
}