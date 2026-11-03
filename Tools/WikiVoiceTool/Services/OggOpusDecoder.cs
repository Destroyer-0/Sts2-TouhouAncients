namespace WikiVoiceTool.Services;

/// <summary>
/// Ogg 容器解复用 + Opus 解码，输出 16bit PCM WAV。
/// Ogg 页可能把一个 packet 拆到下一页（继续标志位），所以按 lacing 累积而不是按页切。
/// </summary>
public static class OggOpusDecoder
{
    private const int SampleRate = 48000;
    private const int MaxFrameSamples = 5760; // 120ms @48k

    public static byte[] ToWav(byte[] ogg)
    {
        var (pcm, channels) = Decode(ogg);
        return BuildWav(pcm, channels);
    }

    public static (short[] Pcm, int Channels) Decode(byte[] ogg)
    {
        var packets = ReadPackets(ogg);
        if (packets.Count == 0 || !IsOpusHead(packets[0]))
        {
            throw new InvalidDataException("不是 Ogg Opus 数据");
        }

        var head = packets[0];
        var channels = head[9];
        if (channels is < 1 or > 2)
        {
            throw new InvalidDataException($"不支持的声道数：{channels}");
        }
        // OpusHead: magic(8) version(1) channels(1) preSkip(2, LE)
        var preSkip = head.Length >= 12 ? (head[10] | (head[11] << 8)) : 0;

        var decoder = Concentus.OpusCodecFactory.CreateDecoder(SampleRate, channels);
        var buffer = new short[MaxFrameSamples * channels];
        var pcm = new List<short>(Square(ogg.Length));

        for (var i = 1; i < packets.Count; i++)
        {
            var packet = packets[i];
            if (packet.Length == 0)
            {
                continue;
            }
            // 第 2 个 packet 固定是 OpusTags，不能送进解码器
            if (i == 1)
            {
                continue;
            }

            var samples = decoder.Decode(packet, buffer, MaxFrameSamples, false);
            if (samples <= 0)
            {
                continue;
            }
            var count = samples * channels;
            for (var k = 0; k < count; k++)
            {
                pcm.Add(buffer[k]);
            }
        }

        var pcmArray = pcm.ToArray();
        var skip = preSkip * channels;
        if (skip > 0 && skip < pcmArray.Length)
        {
            pcmArray = pcmArray[skip..];
        }
        return (pcmArray, channels);
    }

    private static int Square(int length) => length * 2;

    private static bool IsOpusHead(byte[] packet) =>
        packet.Length >= 19 && packet[0] == (byte)'O' && packet[1] == (byte)'p' &&
        packet[2] == (byte)'u' && packet[3] == (byte)'s';

    private static List<byte[]> ReadPackets(byte[] data)
    {
        var packets = new List<byte[]>();
        var packet = new List<byte>(4096);
        var pos = 0;

        while (pos + 27 <= data.Length)
        {
            if (data[pos] != (byte)'O' || data[pos + 1] != (byte)'g' ||
                data[pos + 2] != (byte)'g' || data[pos + 3] != (byte)'S')
            {
                // 页可能被截断或前面有垃圾字节，重新对齐
                pos++;
                continue;
            }

            var segments = data[pos + 26];
            var tableStart = pos + 27;
            var payloadStart = tableStart + segments;
            if (payloadStart > data.Length)
            {
                break;
            }

            var payloadLength = 0;
            for (var i = 0; i < segments; i++)
            {
                payloadLength += data[tableStart + i];
            }
            if (payloadStart + payloadLength > data.Length)
            {
                break;
            }

            var cursor = payloadStart;
            for (var i = 0; i < segments; i++)
            {
                var lacing = data[tableStart + i];
                packet.AddRange(new ArraySegment<byte>(data, cursor, lacing));
                cursor += lacing;
                // lacing < 255 表示这个 packet 在这里结束
                if (lacing < 255)
                {
                    packets.Add(packet.ToArray());
                    packet.Clear();
                }
            }

            pos = payloadStart + payloadLength;
        }

        return packets;
    }

    private static byte[] BuildWav(short[] pcm, int channels)
    {
        var dataSize = pcm.Length * sizeof(short);
        var wav = new byte[44 + dataSize];
        var w = new Span<byte>(wav);

        WriteAscii(w[..4], "RIFF");
        BitConverter.TryWriteBytes(w[4..8], 36 + dataSize);
        WriteAscii(w[8..12], "WAVE");
        WriteAscii(w[12..16], "fmt ");
        BitConverter.TryWriteBytes(w[16..20], 16);
        BitConverter.TryWriteBytes(w[20..22], (short)1); // PCM
        BitConverter.TryWriteBytes(w[22..24], (short)channels);
        BitConverter.TryWriteBytes(w[24..28], SampleRate);
        BitConverter.TryWriteBytes(w[28..32], SampleRate * channels * sizeof(short));
        BitConverter.TryWriteBytes(w[32..34], (short)(channels * sizeof(short)));
        BitConverter.TryWriteBytes(w[34..36], (short)16);
        WriteAscii(w[36..40], "data");
        BitConverter.TryWriteBytes(w[40..44], dataSize);

        for (var i = 0; i < pcm.Length; i++)
        {
            BitConverter.TryWriteBytes(w[(44 + i * 2)..], pcm[i]);
        }
        return wav;
    }

    private static void WriteAscii(Span<byte> target, string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            target[i] = (byte)value[i];
        }
    }
}
