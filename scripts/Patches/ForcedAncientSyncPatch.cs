using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Multiplayer.Transport.ENet;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 强制/禁用 Ancient 的联机同步，仿 RitsuLib RunSavedData：
/// 本机设置只作为主机/单人的提交源；开局时写成局内数据，挂在原版开局包的字节包尾上，
/// 在 GenerateRooms 之前各端读同一份。
/// 不打 LobbyBeginRunMessage / NetMessageBus.SerializeMessage&lt;T&gt;（struct + 泛型 + out 会让 Harmony
/// 生成非法 IL，启动时报 InvalidProgramException）。改打传输层 SendMessageToClient/Host（byte[] + length）。
/// </summary>
public static class ForcedAncientSyncPatch
{
    /// <summary>
    /// 主机/单人开局：先把本机设置提交为局内数据，再发出开局消息。
    /// 客户端不走此方法。
    /// </summary>
    [HarmonyPatch(typeof(StartRunLobby), "BeginRunForAllPlayers")]
    public static class BeginRunForAllPlayers_Prefix
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            AncientRunSavedData.CommitFromLocalConfig();
        }
    }

    /// <summary>
    /// 单人开局可能不经过大厅。这里用本机设置覆盖上一局残留的局内数据。
    /// </summary>
    [HarmonyPatch(typeof(RunManager), "SetUpNewSingleplayer")]
    public static class SetUpNewSingleplayer_Prefix
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            AncientRunSavedData.CommitFromLocalConfig();
        }
    }

    /// <summary>
    /// 生成房间前的兜底：尚未提交则用本机设置。
    /// 客户端若已从包尾 Apply，IsApplied 为 true，不会用本机设置覆盖主机配置。
    /// </summary>
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateRooms))]
    public static class GenerateRooms_Prefix
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            AncientRunSavedData.EnsureAppliedFromLocalIfMissing();
        }
    }

    /// <summary>
    /// 在发出去的字节包上追加 trailer。Priority.First：先于 RitsuLib Native Trailer 写入，
    /// 这样对方包尾仍在最外层，两边都能从后往前找到各自的页脚。
    /// </summary>
    [HarmonyPatch]
    [HarmonyPriority(Priority.First)]
    public static class SendPacket_Prefix
    {
        private static byte? _beginRunMessageId;

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(
                typeof(ENetHost),
                nameof(ENetHost.SendMessageToClient),
                [typeof(ulong), typeof(byte[]), typeof(int), typeof(NetTransferMode), typeof(int)]);
            yield return AccessTools.Method(
                typeof(ENetClient),
                nameof(ENetClient.SendMessageToHost),
                [typeof(byte[]), typeof(int), typeof(NetTransferMode), typeof(int)]);

            Type? steamHost = AccessTools.TypeByName("MegaCrit.Sts2.Core.Multiplayer.Transport.Steam.SteamHost");
            Type? steamClient = AccessTools.TypeByName("MegaCrit.Sts2.Core.Multiplayer.Transport.Steam.SteamClient");
            MethodBase? steamHostSend = steamHost == null
                ? null
                : AccessTools.Method(
                    steamHost,
                    "SendMessageToClient",
                    [typeof(ulong), typeof(byte[]), typeof(int), typeof(NetTransferMode), typeof(int)]);
            MethodBase? steamClientSend = steamClient == null
                ? null
                : AccessTools.Method(
                    steamClient,
                    "SendMessageToHost",
                    [typeof(byte[]), typeof(int), typeof(NetTransferMode), typeof(int)]);
            if (steamHostSend != null)
            {
                yield return steamHostSend;
            }

            if (steamClientSend != null)
            {
                yield return steamClientSend;
            }
        }

        [HarmonyPrefix]
        private static void Prefix(ref byte[] bytes, ref int length)
        {
            if (length <= 0 || bytes == null || bytes.Length < length)
            {
                return;
            }

            try
            {
                _beginRunMessageId ??= (byte)MessageTypes.TypeToId<LobbyBeginRunMessage>();
                if (bytes[0] != _beginRunMessageId.Value)
                {
                    return;
                }

                AncientRunSavedData.CommitFromLocalConfig();
                if (AncientRunSavedData.TryAppendTrailer(ref bytes, ref length))
                {
                    Log.Info("[TouhouAncients] Appended AncientRunSavedData trailer to LobbyBeginRunMessage");
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[TouhouAncients] Failed to append AncientRunSavedData trailer: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 客户端解包原版消息后，从包尾（可能位于其它 Mod trailer 之前）读出主机提交的局内数据。
    /// </summary>
    [HarmonyPatch(typeof(NetMessageBus), nameof(NetMessageBus.TryDeserializeMessage))]
    public static class TryDeserializeMessage_Postfix
    {
        [HarmonyPostfix]
        private static void Postfix(byte[] packetBytes, INetMessage? message, bool __result)
        {
            if (!__result || message is not LobbyBeginRunMessage)
            {
                return;
            }

            try
            {
                if (!AncientRunSavedData.TryReadFromPacket(packetBytes, out AncientRunSavedData? data) || data == null)
                {
                    return;
                }

                data.Apply();
                Log.Info(
                    $"[TouhouAncients] Applied host AncientRunSavedData: forced2={data.ForcedAncient2}, forced3={data.ForcedAncient3}, bannedMask={data.BannedMask}");
            }
            catch (Exception ex)
            {
                Log.Warn($"[TouhouAncients] Failed to read AncientRunSavedData trailer: {ex.Message}");
            }
        }
    }
}
