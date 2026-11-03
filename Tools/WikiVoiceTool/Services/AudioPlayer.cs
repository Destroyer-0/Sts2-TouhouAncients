using System.Runtime.InteropServices;

namespace WikiVoiceTool.Services;

/// <summary>
/// 试听：下载 Ogg Opus -> 进程内解码成 WAV -> winmm PlaySound 异步播放。
/// 不走 WebView2，避开隐藏控件与自动播放策略这两个坑。
/// </summary>
public sealed class AudioPlayer : IDisposable
{
    private const uint SND_ASYNC = 0x0001;
    private const uint SND_NODEFAULT = 0x0002;
    private const uint SND_PURGE = 0x0040;
    private const uint SND_FILENAME = 0x00020000;

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PlaySound(string? sound, IntPtr module, uint flags);

    private static string CacheDir => Path.Combine(Path.GetTempPath(), "WikiVoiceTool", "cache");

    public async Task<bool> PlayAsync(int unit, int style, int no, CancellationToken ct)
    {
        try
        {
            var wav = Path.Combine(CacheDir, $"{unit}_{style}_{no}.wav");
            if (!File.Exists(wav) || new FileInfo(wav).Length <= 44)
            {
                var url = VoiceDownloader.BuildUrl(unit, style, no);
                var ogg = await VoiceDownloader.FetchBytesAsync(url, ct);
                var bytes = OggOpusDecoder.ToWav(ogg);
                Directory.CreateDirectory(CacheDir);
                await File.WriteAllBytesAsync(wav, bytes, ct);
            }
            return PlaySound(wav, IntPtr.Zero, SND_FILENAME | SND_ASYNC | SND_NODEFAULT);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Stop() => PlaySound(null, IntPtr.Zero, SND_PURGE);

    public void Dispose() => Stop();
}
