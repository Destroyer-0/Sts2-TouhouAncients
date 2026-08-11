using BaseLib.Audio;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.encounters;

#pragma warning disable CS0618 // FmodAudio 是 BaseLib 标记为"不建议使用"的封装，但它是当前唯一公开的 FMOD 总线读写入口

namespace TouhouAncients.Scripts;

/// <summary>
/// 为 <see cref="TouhouAncientEncounter"/> 子类（如 <see cref="YorigamiSistersEncounter"/>）
/// 的挑战战斗播放自定义 BGM。子类只需重写 <see cref="TouhouAncientEncounter.BgmFileName"/>。
///
/// 背景：EncounterModel.CustomBgm 只接受 FMOD 事件路径（event:/...），无法直接播放
/// mod 的 mp3 文件——传 res:// 路径会在日志报 "cannot find music path" 且把原 act 音乐停掉。
/// 因此这里改用 BaseLib 的 <see cref="AutoModAudio"/> 在战斗开始时播放 mp3，战斗结束时停止。
/// 循环依赖音频导入设置：对应 mp3 的 import 需开启 loop=true。
///
/// 为避免与游戏原 FMOD 音乐重叠，战斗期间把 FMOD 音乐总线（bus:/master/music）静音
/// （mp3 走 Godot 总线，不受该 FMOD 总线影响），战斗结束后恢复原来的音量。
/// </summary>
public static class EncounterBgm
{
    /// <summary>FMOD 背景音乐总线路径（游戏的"背景音乐"音量滑块即作用于该总线）。</summary>
    private const string MusicBusPath = "bus:/master/music";

    private static readonly AutoModAudio Audio = new("res://debug_audio");

    /// <summary>当前正在播放的 BGM 播放器（由 AutoModAudio.PlayMusic 返回）。</summary>
    private static AudioStreamPlayer? _current;

    /// <summary>战斗开始前记录的 FMOD 音乐总线音量，战斗结束后恢复。</summary>
    private static float _savedMusicBusVolume = 1f;

    /// <summary>是否正处于挑战战斗 BGM 播放中（决定结束后是否需要恢复音乐总线音量）。</summary>
    private static bool _active;

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
        if (state.Encounter is TouhouAncientEncounter encounter && !string.IsNullOrEmpty(encounter.BgmFileName))
        {
            Start(encounter.BgmFileName);
        }
    }

    private static void Start(string bgmFileName)
    {
        Stop();

        // 记录并静音 FMOD 音乐总线，避免与原 act 音乐重叠。
        _savedMusicBusVolume = FmodAudio.GetBusVolume(MusicBusPath);
        FmodAudio.SetBusVolume(MusicBusPath, 0f);

        _current = Audio.PlayMusic(bgmFileName);
        _active = true;
    }

    private static void OnCombatEnded(CombatRoom room)
    {
        Stop();
    }

    /// <summary>
    /// 停止 BGM 播放并恢复 FMOD 音乐总线音量。
    /// </summary>
    public static void Stop()
    {
        if (_current != null && GodotObject.IsInstanceValid(_current))
        {
            _current.Stop();
            _current.GetParent()?.RemoveChild(_current);
        }
        _current = null;

        if (_active)
        {
            FmodAudio.SetBusVolume(MusicBusPath, _savedMusicBusVolume);
            _active = false;
        }
    }
}

#pragma warning restore CS0618
