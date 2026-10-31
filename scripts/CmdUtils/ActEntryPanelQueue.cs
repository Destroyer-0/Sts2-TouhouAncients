using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace TouhouAncients.Scripts.CmdUtils;

/// <summary>
/// 进幕选牌面板的排队闸门：同一 <c>AfterActEntered</c> 时点有多个面板时按调用顺序逐个显示。
/// 地图只在最后一个面板结束时恢复，避免中途重开被玩家点走、以及反复开关的闪烁。
/// </summary>
internal static class ActEntryPanelQueue
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    // 复用同一个 Callable，否则 Disconnect 会因实例不同而失配
    private static readonly Callable MapGuard = Callable.From(CloseMap);

    private static int _pending;
    private static bool _mapHeld;

    public static async Task Run(Player player, Func<Task> show)
    {
        Interlocked.Increment(ref _pending);
        await Gate.WaitAsync();
        try
        {
            HoldMap(player);
            await show();
        }
        finally
        {
            if (Interlocked.Decrement(ref _pending) == 0) ReleaseMap();
            Gate.Release();
        }
    }

    /// <summary>地图盖在 overlay 之上，不关掉面板点不到；只有本机玩家那份需要处理。</summary>
    private static void HoldMap(Player player)
    {
        if (_mapHeld) return;

        NMapScreen? mapScreen = NMapScreen.Instance;
        if (mapScreen == null || !LocalContext.IsMe(player) || !mapScreen.IsOpen) return;

        mapScreen.Close(animateOut: false);
        mapScreen.Connect(NMapScreen.SignalName.Opened, MapGuard);
        _mapHeld = true;
    }

    private static void ReleaseMap()
    {
        if (!_mapHeld) return;
        _mapHeld = false;

        NMapScreen? mapScreen = NMapScreen.Instance;
        if (mapScreen == null || !GodotObject.IsInstanceValid(mapScreen)) return;

        mapScreen.Disconnect(NMapScreen.SignalName.Opened, MapGuard);
        mapScreen.Open();
    }

    private static void CloseMap() => NMapScreen.Instance?.Close(animateOut: false);
}
