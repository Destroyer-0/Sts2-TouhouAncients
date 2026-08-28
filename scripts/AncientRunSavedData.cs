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
    public const ushort PayloadVersion = 1;
    public const int TrailerSize = 26;

    private static readonly byte[] Header = "TAS1"u8.ToArray();
    private static readonly byte[] Footer = "1SAT"u8.ToArray();

    public ForcedAncientOption ForcedAncient2 { get; init; }
    public ForcedAncientOption ForcedAncient3 { get; init; }
    public ulong BannedMask { get; init; }

    /// <summary>本局是否已提交过（网络包尾或本机配置）。</summary>
    public static bool IsApplied { get; private set; }

    public static AncientRunSavedData FromLocalConfig()
    {
        return new AncientRunSavedData
        {
            ForcedAncient2 = TouhouAncientsConfig.ForcedAncient_2,
            ForcedAncient3 = TouhouAncientsConfig.ForcedAncient_3,
            BannedMask = TouhouAncientsConfig.BuildBannedMask()
        };
    }

    public void Apply()
    {
        TouhouAncientsConfig.ForcedAncient_2_Run = ForcedAncient2;
        TouhouAncientsConfig.ForcedAncient_3_Run = ForcedAncient3;
        TouhouAncientsConfig.BannedMask_Run = BannedMask;
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
        Footer.CopyTo(trailer, 22);
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
        if (packetBytes == null || length < TrailerSize || packetBytes.Length < length)
        {
            return false;
        }

        for (int footerStart = length - Footer.Length; footerStart >= TrailerSize - Footer.Length; footerStart--)
        {
            if (!packetBytes.AsSpan(footerStart, Footer.Length).SequenceEqual(Footer))
            {
                continue;
            }

            int start = footerStart - (TrailerSize - Footer.Length);
            if (start < 0)
            {
                continue;
            }

            if (!TryDecodeTrailer(packetBytes.AsSpan(start, TrailerSize), out data))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool TryDecodeTrailer(ReadOnlySpan<byte> trailer, out AncientRunSavedData? data)
    {
        data = null;
        if (trailer.Length != TrailerSize)
        {
            return false;
        }

        if (!trailer[..Header.Length].SequenceEqual(Header))
        {
            return false;
        }

        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(trailer.Slice(4, 2));
        if (version != PayloadVersion)
        {
            Log.Warn($"[TouhouAncients] AncientRunSavedData unsupported version {version}");
            return false;
        }

        data = new AncientRunSavedData
        {
            ForcedAncient2 = (ForcedAncientOption)BinaryPrimitives.ReadInt32LittleEndian(trailer.Slice(6, 4)),
            ForcedAncient3 = (ForcedAncientOption)BinaryPrimitives.ReadInt32LittleEndian(trailer.Slice(10, 4)),
            BannedMask = BinaryPrimitives.ReadUInt64LittleEndian(trailer.Slice(14, 8))
        };
        return true;
    }
}
