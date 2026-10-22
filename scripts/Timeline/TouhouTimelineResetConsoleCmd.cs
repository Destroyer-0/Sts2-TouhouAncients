using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace TouhouAncients.Scripts.Timeline;

/// <summary>清空本 Mod 时间线节点的全部进度，回到首次安装状态。</summary>
public sealed class TouhouTimelineResetConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "touhou-timeline-reset";

    public override string Args => "";

    public override string Description => "Clears all TouhouAncients timeline (epoch) progress on the current save.";

    public override bool IsNetworked => false;

    // 重置存档类命令，非调试模式也要能用
    public override bool DebugOnly => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        int removed = TouhouAncientTimelineRegistry.ClearAllProgress();

        // 局内玩家身上还带着旧 epoch，不清会在结算页/写 replay 时继续出现
        foreach (Player player in RunManager.Instance.DebugOnlyGetState()?.Players ?? [])
        {
            player.DiscoveredEpochs.RemoveAll(TouhouAncientTimelineRegistry.RegisteredEpochIds.Contains);
        }

        SaveManager.Instance.SaveProgressFile();
        return new CmdResult(success: true, $"Cleared {removed} TouhouAncients timeline epoch(s).");
    }
}
