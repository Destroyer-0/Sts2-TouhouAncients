using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 修复"强制出现先古之民"与"禁用先古之民"联机不同步：
/// BaseLib 配置是各端本地文件，主机与客户端的配置互不相通，
/// 导致各端在 ActModel.GenerateRooms 中读到不同配置、刷出不同 Ancient
/// （强制：ShouldForceSpawn 替换结果不同；禁用：候选池不同 → RNG 索引错位）。
///
/// 方案：开局广播 LobbyBeginRunMessage 时，主机把自己的强制配置与禁用掩码写入运行期字段
/// （ForcedAncient_2_Run / ForcedAncient_3_Run / BannedMask_Run），随消息同步到所有客户端；
/// ShouldForceSpawn 只读取运行期强制字段，BanAncientPatch 过滤只读取运行期禁用掩码，
/// 各端一致。非主机端的本地设置文件不被修改。
///
/// 时序（各端均先于 GenerateRooms 生效）：
/// - 主机：BeginRunForAllPlayers Prefix 写入运行期字段 → Serialize Postfix 追加到消息
/// - 客户端：Deserialize Postfix 从消息读出主机配置写入运行期字段
/// - 单人：走主机路径，运行期字段 = 本地配置，行为不变
/// </summary>
public static class ForcedAncientSyncPatch
{
    /// <summary>追加数据的 magic 标记，用于识别新版主机消息（旧版消息无追加数据）。</summary>
    private const int Magic = 0x54414631; // "TAF1"

    /// <summary>
    /// 开局瞬间（主机/单人）：把本地配置拷贝到运行期字段。
    /// 客户端不经过此方法（通过消息接收），其运行期字段在 Deserialize 时被覆盖为主机配置。
    /// </summary>
    [HarmonyPatch(typeof(StartRunLobby), "BeginRunForAllPlayers")]
    public static class BeginRunForAllPlayers_Prefix
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            TouhouAncientsConfig.ForcedAncient_2_Run = TouhouAncientsConfig.ForcedAncient_2;
            TouhouAncientsConfig.ForcedAncient_3_Run = TouhouAncientsConfig.ForcedAncient_3;
            TouhouAncientsConfig.BannedMask_Run = TouhouAncientsConfig.BuildBannedMask();
        }
    }

    /// <summary>
    /// 主机广播开局消息时：把运行期字段追加写入消息尾部。
    /// Harmony Postfix 在方法返回前执行，SerializeMessage 随后按更新后的 BitPosition
    /// 计算消息长度，追加数据会被完整包含在消息中。
    /// 格式：Magic(int) + ForcedAncient_2(int) + ForcedAncient_3(int)
    ///      + BannedMaskPresent(bool) + BannedMask(ulong)
    /// </summary>
    [HarmonyPatch(typeof(LobbyBeginRunMessage), nameof(LobbyBeginRunMessage.Serialize))]
    public static class LobbyBeginRunMessage_Serialize_Postfix
    {
        [HarmonyPostfix]
        private static void Postfix(PacketWriter writer)
        {
            writer.WriteInt(Magic);
            writer.WriteInt((int)TouhouAncientsConfig.ForcedAncient_2_Run);
            writer.WriteInt((int)TouhouAncientsConfig.ForcedAncient_3_Run);
            var mask = TouhouAncientsConfig.BannedMask_Run;
            writer.WriteBool(mask.HasValue);
            writer.WriteULong(mask ?? 0UL);
        }
    }

    /// <summary>
    /// 客户端收到开局消息时：读取出主机的强制配置与禁用掩码并写入运行期字段。
    /// 用 magic 识别新版主机消息；旧版主机消息（无追加数据）在消息末尾读取会越界，
    /// 捕获异常后保持默认值（不强制、不禁用），不影响其他字段。
    /// </summary>
    [HarmonyPatch(typeof(LobbyBeginRunMessage), nameof(LobbyBeginRunMessage.Deserialize))]
    public static class LobbyBeginRunMessage_Deserialize_Postfix
    {
        [HarmonyPostfix]
        private static void Postfix(PacketReader reader)
        {
            try
            {
                if (reader.ReadInt() != Magic)
                {
                    return;
                }
                TouhouAncientsConfig.ForcedAncient_2_Run = (ForcedAncientOption)reader.ReadInt();
                TouhouAncientsConfig.ForcedAncient_3_Run = (ForcedAncientOption)reader.ReadInt();
                if (reader.ReadBool())
                {
                    TouhouAncientsConfig.BannedMask_Run = reader.ReadULong();
                }
            }
            catch (Exception)
            {
                // 旧版主机消息没有追加数据，读取越界；忽略并保持默认值。
            }
        }
    }
}
