using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.encounters;

/// <summary>
/// 东方角色挑战战斗的 Encounter 基类，统一处理自定义 BGM。
///
/// 子类只需重写 <see cref="BgmFileName"/> 指定自己的 BGM 文件，战斗开始/结束时由
/// <see cref="EncounterBgm"/>（scripts/EncounterBgm.cs）自动播放/停止——Encounter
/// 本身不接收战斗 Hook，故由全局订阅 CombatManager 事件的 EncounterBgm 统一处理。
/// </summary>
public abstract class TouhouAncientEncounter : CustomEncounterModel
{
    protected TouhouAncientEncounter(RoomType roomType) : base(roomType)
    {
    }

    /// <summary>
    /// 自定义 BGM 文件名（位于 res://debug_audio/，也可填完整的 res:// 路径）。
    /// 返回 null 表示不播放自定义 BGM。注意：对应音频文件的 import 需开启 loop=true 才能循环。
    /// </summary>
    public virtual string? BgmFileName => null;
}
