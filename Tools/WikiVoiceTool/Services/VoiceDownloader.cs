using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using WikiVoiceTool.Models;

namespace WikiVoiceTool.Services;

public enum DownloadResult
{
    Skipped,
    Downloaded,
    Failed,
}

/// <summary>URL 拼装、下载、重试、命名、manifest。</summary>
public static class VoiceDownloader
{
    private const string Site = "https://eikaebisu.moe";
    private static readonly HttpClient Http = CreateClient();
    private static readonly Regex IllegalRe = new("[\\\\/:*?\"<>|\\x00-\\x1f]", RegexOptions.Compiled);

    public static string BuildUrl(int unit, int styleFile, int no) =>
        $"{Site}/audios/assets/east/sounds/voice/{unit}/{styleFile}/{no}.opus";

    /// <summary>{输出}\{unit}\{no:00}_{标题}_{声线}_{CV}.opus</summary>
    public static string BuildPath(string outDir, int unit, VoiceLine line, VoiceStyle style) =>
        Path.Combine(outDir, unit.ToString(),
            $"{line.No:00}_{Safe(line.Title)}_{style.File}_{Safe(style.Cv)}.opus");

    public static string Safe(string? text)
    {
        var v = IllegalRe.Replace(text ?? "", "_").Trim();
        return string.IsNullOrWhiteSpace(v) ? "(未命名)" : v;
    }

    /// <summary>已存在且非空视为成功（支持断点式重跑）。失败重试 3 次，指数退避。</summary>
    public static async Task<DownloadResult> DownloadAsync(string url, string path, CancellationToken ct)
    {
        try
        {
            if (File.Exists(path) && new FileInfo(path).Length > 0)
            {
                return DownloadResult.Skipped;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            for (var attempt = 0; attempt < 3; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await Task.Delay(100, ct); // 轻限速，避免压站点
                    using var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                    resp.EnsureSuccessStatusCode();
                    var tmp = path + ".part";
                    await using (var fs = File.Create(tmp))
                    {
                        await resp.Content.CopyToAsync(fs, ct);
                    }
                    File.Move(tmp, path, overwrite: true);
                    return DownloadResult.Downloaded;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception) when (attempt < 2)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1 << attempt), ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // 落到失败
        }
        return DownloadResult.Failed;
    }

    /// <summary>读完整字节（解码试听用），失败重试 3 次。</summary>
    public static async Task<byte[]> FetchBytesAsync(string url, CancellationToken ct)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var resp = await Http.GetAsync(url, ct);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsByteArrayAsync(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(TimeSpan.FromSeconds(1 << attempt), ct);
            }
        }
        throw new InvalidOperationException($"音频读取失败：{last?.Message}", last);
    }

    /// <summary>UTF-8 无 BOM，中文不转义，便于人工校对。</summary>
    public static void WriteManifest(VoicePage page, string outDir)
    {
        var dir = Path.Combine(outDir, page.UnitId.ToString());
        Directory.CreateDirectory(dir);
        var model = new
        {
            unit = page.UnitId,
            character = page.CharacterName,
            styles = page.Styles.Select(s => new { file = s.File, cv = s.Cv, displayName = s.DisplayName }),
            lines = page.Lines.Select(l => new { no = l.No, title = l.Title, hasAudio = l.HasAudio, text = l.Text }),
        };
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
        File.WriteAllText(Path.Combine(dir, "manifest.json"), json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("WikiVoiceTool/1.0");
        return http;
    }
}
