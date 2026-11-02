using System;
using System.Buffers.Binary;
using MegaCrit.Sts2.Core.Logging;

namespace TouhouAncients.Scripts;

/// <summary>
/// 本局实际生效的强制/禁用 Ancient 数据（仿 RitsuLib RunSavedData：局内一份、开局提交）。
/// 本机 SimpleModConfig 只作为主机/单人的提交源；客户端用开局消息包尾覆盖，不改本地设置文件。
/// </summary>
public sealed class AncientRunSavedData
{
    public const ushort PayloadVersion = 2;
    public const int TrailerSize = 27;

    /// <summary>v1 包尾（无回血比例）。仍读，兼容旧版客户端/主机，读到时按默认 35 处理。</summary>
    private const ushort PayloadVersionV1 = 1;
    private const int TrailerSizeV1 = 26;
    private const int DefaultHealPercent = 35;

    private static readonly byte[] Header = "TAS1"u8.ToArray();
    private static readonly byte[] Footer = "1SAT"u8.ToArray();

    public ForcedAncientOption ForcedAncient2 { get; init; }
    public ForcedAncientOption ForcedAncient3 { get; init; }
    public ulong BannedMask { get; init; }

    /// <summary>本局生效的挑战回血比例（%）。v1 包尾没有该字段，默认 35。</summary>
    public int ChallengeHealPercent { get; init; } = DefaultHealPercent;

    /// <summary>本局是否已提交过（网络包尾或本机配置）。</summary>
    public static bool IsApplied { get; private set; }

    public static AncientRunSavedData FromLocalConfig()
    {
        return new AncientRunSavedData
        {
            ForcedAncient2 = TouhouAncientsConfig.ForcedAncient_2,
            ForcedAncient3 = TouhouAncientsConfig.ForcedAncient_3,
            BannedMask = TouhouAncientsConfig.BuildBannedMask(),
            ChallengeHealPercent = TouhouAncientsConfig.ChallengeHealPercent
        };
    }

    public void Apply()
    {
        TouhouAncientsConfig.ForcedAncient_2_Run = ForcedAncient2;
        TouhouAncientsConfig.ForcedAncient_3_Run = ForcedAncient3;
        TouhouAncientsConfig.BannedMask_Run = BannedMask;
        TouhouAncientsConfig.ChallengeHealPercent_Run = Math.Clamp(ChallengeHealPercent, 0, 100);
        IsApplied = true;
    }

    /// <summary>
    /// 主机/单人开局：把本机设置提交为局内数据。
    /// 客户端已从包尾 Apply 过则不要再调，否则会盖掉主机配置。
    /// </summary>
    public static void CommitFromLocalConfig()
    {
        FromLocalConfig().Apply();
    }

    /// <summary>
    /// GenerateRooms 前的兜底：若尚未提交（单人未走大厅、或旧版主机没带包尾），用本机设置。
    /// </summary>
    public static void EnsureAppliedFromLocalIfMissing()
    {
        if (!IsApplied)
        {
            CommitFromLocalConfig();
        }
    }


    public byte[] EncodeTrailer()
    {
        byte[] trailer = new byte[TrailerSize];
        Header.CopyTo(trailer, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(trailer.AsSpan(4, 2), PayloadVersion);
        BinaryPrimitives.WriteInt32LittleEndian(trailer.AsSpan(6, 4), (int)ForcedAncient2);
        BinaryPrimitives.WriteInt32LittleEndian(trailer.AsSpan(10, 4), (int)ForcedAncient3);
        BinaryPrimitives.WriteUInt64LittleEndian(trailer.AsSpan(14, 8), BannedMask);
        trailer[22] = (byte)Math.Clamp(ChallengeHealPercent, 0, 100);
        Footer.CopyTo(trailer, 23);
        return trailer;
    }

    public static bool TryAppendTrailer(ref byte[] packetBytes, ref int length)
    {
        if (length <= 0 || packetBytes.Length < length)
        {
            return false;
        }

        if (TryReadFromPacket(packetBytes, length, out _))
        {
            return false;
        }

        byte[] trailer = FromLocalConfig().EncodeTrailer();
        byte[] next = new byte[length + trailer.Length];
        Buffer.BlockCopy(packetBytes, 0, next, 0, length);
        Buffer.BlockCopy(trailer, 0, next, length, trailer.Length);
        packetBytes = next;
        length = next.Length;
        return true;
    }

    public static bool TryReadFromPacket(byte[] packetBytes, out AncientRunSavedData? data)
    {
        return TryReadFromPacket(packetBytes, packetBytes?.Length ?? 0, out data);
    }

    public static bool TryReadFromPacket(byte[] packetBytes, int length, out AncientRunSavedData? data)
    {
        data = null;
        if (packetBytes == null || length < TrailerSizeV1 || packetBytes.Length < length)
        {
            return false;
        }

        // 先按 v2 长度试，失败再按 v1（旧主机发的包尾）试，两者 Footer 相同。
        for (int footerStart = length - Footer.Length; footerStart >= 0; footerStart--)
        {
            if (!packetBytes.AsSpan(footerStart, Footer.Length).SequenceEqual(Footer))
            {
                continue;
            }

            int startV2 = footerStart - (TrailerSize - Footer.Length);
            if (startV2 >= 0 && TryDecodeTrailer(packetBytes.AsSpan(startV2, TrailerSize), out data))
            {
                return true;
            }

            int startV1 = footerStart - (TrailerSizeV1 - Footer.Length);
            if (startV1 >= 0 && TryDecodeTrailer(packetBytes.AsSpan(startV1, TrailerSizeV1), out data))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryDecodeTrailer(ReadOnlySpan<byte> trailer, out AncientRunSavedData? data)
    {
        data = null;
        if (trailer.Length < TrailerSizeV1 || !trailer[..Header.Length].SequenceEqual(Header))
        {
            return false;
        }

        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(trailer.Slice(4, 2));
        bool isV2 = version == PayloadVersion && trailer.Length == TrailerSize;
        if (!isV2 && !(version == PayloadVersionV1 && trailer.Length == TrailerSizeV1))
        {
            Log.Warn($"[TouhouAncients] AncientRunSavedData unsupported version {version}");
            return false;
        }

        data = new AncientRunSavedData
        {
            ForcedAncient2 = (ForcedAncientOption)BinaryPrimitives.ReadInt32LittleEndian(trailer.Slice(6, 4)),
            ForcedAncient3 = (ForcedAncientOption)BinaryPrimitives.ReadInt32LittleEndian(trailer.Slice(10, 4)),
            BannedMask = BinaryPrimitives.ReadUInt64LittleEndian(trailer.Slice(14, 8)),
            ChallengeHealPercent = isV2 ? trailer[22] : DefaultHealPercent
        };
        return true;
    }
}
