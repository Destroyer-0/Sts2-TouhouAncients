using BaseLib.Audio;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.encounters;

namespace TouhouAncients.Scripts;

/// <summary>
/// 为 <see cref="TouhouAncientEncounter"/> 子类（如 <see cref="YorigamiSistersEncounter"/>）
/// 的挑战战斗播放自定义 BGM。子类只需重写 <see cref="TouhouAncientEncounter.BgmFileName"/>。
///
/// 背景：EncounterModel.CustomBgm 只接受 FMOD 事件路径（event:/...），无法直接播放
/// mod 的 mp3 文件——传 res:// 路径会在日志报 "cannot find music path" 且把原 act 音乐停掉。
/// 因此这里改用 BaseLib 的 <see cref="AutoModAudio"/> 播放 mp3，战斗结束时停止。
/// 默认在战斗开始时播放；子类可将 <see cref="TouhouAncientEncounter.AutoStartBgm"/> 设为 false，
/// 开场只停掉原版音乐，稍后调用 <see cref="Start"/> 再播（可淡入）。循环依赖音频导入设置：对应 mp3 的 import 需开启 loop=true。
///
/// 为避免与游戏原 FMOD 音乐重叠，战斗期间停掉局内 act 音乐事件，结束再重建。
/// 不用 SetBusMute：总线静音是全局状态，会活过场景销毁，而"保存并退出"走
/// RunManager.CleanUp → CombatManager.Reset 不触发 CombatEnded → 主线永久静音
/// （连主菜单都没音乐）。事件实例随 NRun 销毁，天然无残留。
/// mp3 走 Godot Master 总线，与原版 FMOD 无关。
/// </summary>
public static class EncounterBgm
{
    private static readonly AutoModAudio Audio = new("res://debug_audio");

    /// <summary>当前正在播放的 BGM 播放器（由 AutoModAudio.PlayMusic 返回）。</summary>
    private static AudioStreamPlayer? _current;

    /// <summary>是否已停掉原版 act 音乐，决定结束后是否恢复。</summary>
    private static bool _active;

    /// <summary>
    /// 局内音乐代理节点（<c>NRunMusicController</c> 的子节点 "Proxy"）。
    /// 只暴露 <c>stop_music</c> 这种"停事件、保留 _currentTrack"的操作，供恢复时按原曲重建。
    /// </summary>
    private static Node? MusicProxy => NRunMusicController.Instance?.GetNodeOrNull<Node>("Proxy");

    /// <summary>
    /// 订阅战斗开始/结束事件。在 <see cref="Entry.Init"/> 中调用一次。
    /// </summary>
    public static void Initialize()
    {
        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    private static void OnCombatSetUp(CombatState state)
    {
        if (state.Encounter is not TouhouAncientEncounter encounter || string.IsNullOrEmpty(encounter.BgmFileName))
        {
            return;
        }

        if (encounter.AutoStartBgm)
        {
            Start(encounter.BgmFileName, 2);
        }
        else
        {
            TouhouAncientEncounterBgmNamePatch.TryHide();
            StopVanillaMusic();
        }
    }

    /// <summary>
    /// 开始播放自定义 BGM。<paramref name="fadeInSeconds"/> 大于 0 时从静音淡入到正常音量。
    /// </summary>
    public static void Start(string bgmFileName, float fadeInSeconds = 0f)
    {
        StopCurrentPlayer();
        StopVanillaMusic();

        _current = Audio.PlayMusic(bgmFileName);
        if (_current != null && fadeInSeconds > 0f)
        {
            _current.FadeIn(fadeInSeconds);
        }

        TouhouAncientEncounterBgmNamePatch.TryShow();
    }

    private static void OnCombatEnded(CombatRoom room)
    {
        Stop();
    }

    /// <summary>
    /// 停止 BGM 播放并恢复原版 act 音乐。
    /// </summary>
    public static void Stop()
    {
        StopCurrentPlayer();
        RestoreVanillaMusic();
    }

    private static void StopCurrentPlayer()
    {
        if (_current != null && GodotObject.IsInstanceValid(_current))
        {
            _current.Stop();
            _current.GetParent()?.RemoveChild(_current);
        }
        _current = null;
    }

    /// <summary>
    /// 停掉局内 act 音乐事件。只停事件、不动 _currentTrack，
    /// 这样 RestoreVanillaMusic 才能按原曲重建。
    /// </summary>
    private static void StopVanillaMusic()
    {
        if (_active)
        {
            return;
        }

        MusicProxy?.Call("stop_music");
        _active = true;
    }

    private static void RestoreVanillaMusic()
    {
        if (!_active)
        {
            return;
        }

        _active = false;
        // StopCustomMusic 会 stop + 重建 _currentTrack 并按 Progress=CombatEnd 定位。
        NRunMusicController.Instance?.StopCustomMusic();
    }
}
