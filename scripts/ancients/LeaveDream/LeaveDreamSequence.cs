using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace TouhouAncients.Scripts;

/// <summary>
/// 「离开梦境」的演出：哆来咪用对话形式说一句告别台词（带"继续"按钮），
/// 玩家点"继续"后当前场景逐渐溶解，露出已经就位的涅奥房间。
///
/// 只在选择者本机执行；其他玩家的画面/事件实例完全不受影响。
/// </summary>
internal static class LeaveDreamSequence
{
    /// <summary>台词滑入动画时长（原版内容区是 1 秒补间）；这段时间内不开放推进，否则台词等于没播。</summary>
    private const float DialogueSlideInSeconds = 1.0f;

    /// <summary>推进台词后到开始溶解之间的停顿，让末行字幕（「梦境逐渐溶解……」）浮现一下。</summary>
    private const float CaptionSeconds = 0.45f;

    /// <summary>等待玩家点「继续」的信号源；由对话推进补丁（LeaveDreamPatches）触发。</summary>
    private static TaskCompletionSource? _pendingContinue;

    /// <summary>由对话推进补丁调用：玩家点了一下对话推进。</summary>
    public static void NotifyDialogueAdvanced()
    {
        _pendingContinue?.TrySetResult();
    }

    /// <summary>演出总入口：先播对话，再溶解换景。</summary>
    public static async Task Play(Neow neow, Player player)
    {
        var oldRoom = NEventRoom.Instance;
        var layout = oldRoom?.Layout as NAncientEventLayout;
        if (oldRoom == null || layout == null)
        {
            Log.Warn("[TouhouAncients] 离开梦境：拿不到事件房间布局，跳过演出（模型层已经切换到涅奥）。");
            return;
        }

        await PlayDialogue(layout);
        await SwitchToNeowRoom(neow, player, oldRoom);
    }

    /// <summary>
    /// 播放哆来咪的告别台词并等待玩家点"继续"。
    /// 台词写两行：第 1 行是告别台词（带「继续」），第 2 行是「梦境逐渐溶解……」这句末行字幕
    /// ——原版规则下"最后一行"不显示继续按钮，所以必须留第二行占位。
    /// </summary>
    private static async Task PlayDialogue(NAncientEventLayout layout)
    {
        // 对话期间禁止点选项（进房时的按钮可能已被 AnimateButtonsIn 启用）。
        layout.DisableEventOptions();

        var line = new AncientDialogueLine("")
        {
            LineText = new LocString("ancients", LeaveDreamReentry.DialogueLineKey),
            NextButtonText = new LocString("ancients", LeaveDreamReentry.DialogueNextKey),
            Speaker = AncientDialogueSpeaker.Ancient,
        };
        var caption = new AncientDialogueLine("")
        {
            LineText = new LocString("ancients", LeaveDreamReentry.DialogueSentinelKey),
            Speaker = AncientDialogueSpeaker.Ancient,
        };

        // 先清掉对话容器里残留的入场台词。原版点完选项会走 EventModel.SetEventState
        // → EventRoom.RefreshEventState → ClearDialogue；「离开梦境」不改变事件状态，
        // 所以这一步必须自己做，否则 SetDialogue 只是把新台词追加在旧台词之后，
        // 第 0 行仍然是旧台词（点了选项台词不变），第 1 行才闪出告别台词并立刻切到涅奥，
        // 末行「梦境逐渐溶解……」永远不会显示。
        layout.ClearDialogue();

        _pendingContinue = new TaskCompletionSource();
        layout.SetDialogue(new List<AncientDialogueLine> { line, caption });
        // 播放第 1 行（同时会按原版规则显示「继续」并启用推进热区）。
        layout.OnSetupComplete();
        layout.DisableEventOptions();

        // 内容是从下方滑入的（原版 1 秒补间），滑入期间先关掉推进热区，
        // 否则玩家随手一点（推进热区覆盖面很大）就跳到末行，台词等于没播。
        var hitbox = layout.GetNodeOrNull<NAncientDialogueHitbox>("%DialogueHitbox");
        if (hitbox != null)
        {
            hitbox.Disable();
            hitbox.Visible = false;
        }
        else
        {
            Log.Warn("[TouhouAncients] 离开梦境：未找到对话推进热区，只能依靠推进补丁触发（滑入期点击可能跳过台词）。");
        }

        await WaitSeconds(layout, DialogueSlideInSeconds);

        if (hitbox != null)
        {
            hitbox.Visible = true;
            hitbox.Enable();
        }

        try
        {
            await _pendingContinue.Task;
        }
        finally
        {
            _pendingContinue = null;
        }

        // 原版在推进到"最后一行"时会重新启用选项按钮，这里立刻收回，
        // 并直接掐掉鼠标命中，避免溶解那一秒内误点到哆来咪的遗物选项。
        layout.DisableEventOptions();
        foreach (var optionButton in layout.OptionButtons)
        {
            optionButton.MouseFilter = Control.MouseFilterEnum.Ignore;
        }

        // 让末行字幕浮现一下再开始溶解（截图会把它一起截进去）。
        await WaitSeconds(layout, CaptionSeconds);
    }

    /// <summary>等待指定秒数；节点被释放时直接返回。</summary>
    private static async Task WaitSeconds(Node node, float seconds)
    {
        if (!GodotObject.IsInstanceValid(node))
        {
            return;
        }

        var tree = node.GetTree();
        if (tree == null)
        {
            return;
        }

        await node.ToSignal(tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    /// <summary>
    /// 建好涅奥房间并把它放到溶解覆盖层之下，等溶解结束后登记为房间容器的当前场景。
    /// </summary>
    private static async Task SwitchToNeowRoom(Neow neow, Player player, NEventRoom oldRoom)
    {
        var container = oldRoom.GetParent();
        if (container == null)
        {
            Log.Error("[TouhouAncients] 离开梦境：事件房间不在房间容器里，无法切换画面。");
            return;
        }

        await PreloadManager.LoadRoomEventAssets(ModelDb.Event<Neow>(), player.RunState);

        var newRoom = NEventRoom.Create(neow, player.RunState, isPreFinished: false);
        if (newRoom == null)
        {
            Log.Warn("[TouhouAncients] 离开梦境：创建涅奥房间节点失败（可能处于 TestMode），跳过画面切换。");
            return;
        }

        // 先挂进房间容器：位于旧房间之下，溶解过程中会被覆盖层逐渐露出来。
        container.AddChildSafely(newRoom);
        container.MoveChild(newRoom, 0);

        // 溶解：覆盖层显示旧房间的实时画面，用原版 dissolve 着色器把 threshold 从 1 补间到 0，
        // 逐渐溶解掉旧房间、露出下面已经就位的涅奥房间。
        // 演出结束后覆盖层（连同被搬进它的旧房间）由 Play 自己释放，容器里就只剩涅奥房间。
        await NLeaveDreamDissolve.Play(oldRoom, newRoom);

        RegisterCurrentRoom(container, newRoom);
    }

    /// <summary>
    /// 把一个**已经挂在房间容器里**的房间登记为容器的当前场景，不触碰容器里的其他子节点。
    ///
    /// 这里刻意不用 <c>NRun.SetCurrentRoom</c>：它会走 <c>NSceneContainer.SetCurrentScene</c>，
    /// 而后者会先把容器里**所有**子节点 RemoveChild + QueueFree —— 包括我们的涅奥房间。
    /// 若为了避开这一点而先把涅奥房间摘出容器，它会离开场景树，而
    /// <c>NEventRoom._ExitTree</c> 会执行 <c>_cts.Cancel()</c>、退订 <c>_event.StateChanged</c>
    /// 与各选项的 <c>BeforeChosen</c>；再次进树时 <c>_Ready</c> 不会重跑，这些订阅无法恢复，
    /// 结果是涅奥房间点了选项之后界面再也不刷新（表现为卡死）。
    ///
    /// 涅奥房间从加入容器起就没有离开过场景树，因此这里只需补上「登记」这一步：
    /// 写入容器的当前场景字段，并让 <c>ActiveScreenContext</c> 重新评估输入目标。
    /// </summary>
    private static void RegisterCurrentRoom(Node container, NEventRoom room)
    {
        var field = AccessTools.Field(typeof(NSceneContainer), "_currentScene");
        if (field == null)
        {
            Log.Error("[TouhouAncients] 离开梦境：无法解析 NSceneContainer._currentScene，"
                      + "涅奥房间没有登记为当前场景（原版后续取事件房间时可能拿到 null）。");
        }
        else
        {
            field.SetValue(container, room);
        }

        ActiveScreenContext.Instance.Update();
    }
}
