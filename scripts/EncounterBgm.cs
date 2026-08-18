using BaseLib.Audio;
using BaseLib.Extensions;
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
/// 因此这里改用 BaseLib 的 <see cref="AutoModAudio"/> 播放 mp3，战斗结束时停止。
/// 默认在战斗开始时播放；子类可将 <see cref="TouhouAncientEncounter.AutoStartBgm"/> 设为 false，
/// 开场只静音原版 FMOD 音乐，稍后调用 <see cref="Start"/> 再播（可淡入）。循环依赖音频导入设置：对应 mp3 的 import 需开启 loop=true。
///
/// 为避免与游戏原 FMOD 音乐重叠，战斗期间用 <see cref="FmodAudio.SetBusMute"/> 静音
/// FMOD 音乐总线（bus:/master/music）。静音与音量独立：滑块仍写入 VolumeBgm 和总线音量，
/// mp3 由 BaseLib <c>ModAudio.UpdateVolumes</c> 跟着 VolumeBgm 变；结束时 unmute 即可，
/// 不必快照或回写音量。mp3 走 Godot Master 总线，不受该 FMOD mute 影响。
/// </summary>
public static class EncounterBgm
{
    /// <summary>FMOD 背景音乐总线路径（游戏的"背景音乐"音量滑块即作用于该总线）。</summary>
    private const string MusicBusPath = "bus:/master/music";

    private static readonly AutoModAudio Audio = new("res://debug_audio");

    /// <summary>当前正在播放的 BGM 播放器（由 AutoModAudio.PlayMusic 返回）。</summary>
    private static AudioStreamPlayer? _current;

    /// <summary>是否已用 SetBusMute 静音原版 FMOD 音乐，决定结束后是否 unmute。</summary>
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
        if (state.Encounter is not TouhouAncientEncounter encounter || string.IsNullOrEmpty(encounter.BgmFileName))
        {
            return;
        }

        if (encounter.AutoStartBgm)
        {
            Start(encounter.BgmFileName);
        }
        else
        {
            TouhouAncientEncounterBgmNamePatch.TryHide();
            MuteVanillaMusic();
        }
    }

    /// <summary>
    /// 开始播放自定义 BGM。<paramref name="fadeInSeconds"/> 大于 0 时从静音淡入到正常音量。
    /// </summary>
    public static void Start(string bgmFileName, float fadeInSeconds = 0f)
    {
        StopCurrentPlayer();
        MuteVanillaMusic();

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
    /// 停止 BGM 播放并解除 FMOD 音乐总线静音。
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
    /// 静音 FMOD 音乐总线。用 mute 而不是把音量写成 0，这样滑块仍可改 VolumeBgm / 总线音量。
    /// </summary>
    private static void MuteVanillaMusic()
    {
        if (_active)
        {
            return;
        }

        FmodAudio.SetBusMute(MusicBusPath, true);
        _active = true;
    }

    private static void RestoreVanillaMusic()
    {
        if (!_active)
        {
            return;
        }

        FmodAudio.SetBusMute(MusicBusPath, false);
        _active = false;
    }
}

#pragma warning restore CS0618
