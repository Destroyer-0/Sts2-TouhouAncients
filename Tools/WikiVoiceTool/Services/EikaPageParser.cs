using System.Net;
using System.Text.RegularExpressions;
using WikiVoiceTool.Models;

namespace WikiVoiceTool.Services;

/// <summary>eikaebisu.moe 角色页解析。语音元数据全在首屏 HTML，无需跑 JS。</summary>
public static class EikaPageParser
{
    private const string Site = "https://eikaebisu.moe";
    private const RegexOptions Opt = RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled;

    private static readonly HttpClient Http = CreateClient();
    private static readonly Regex TagRe = new(@"<[^>]+>", Opt);
    private static readonly Regex SpacesRe = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex ButtonRe = new(@"<button\b[^>]*>", Opt);
    private static readonly Regex StyleNameRe = new(@"<span[^>]*class=""[^""]*style-name[^""]*""[^>]*>(?<v>.*?)</span>", Opt);
    private static readonly Regex VoicePlayRe = new(@"<button\b[^>]*class=""[^""]*voice-play[^""]*""", Opt);
    private static readonly Regex TypeTextRe = new(@"<span[^>]*class=""[^""]*voice-type-text[^""]*""[^>]*>(?<v>.*?)</span>", Opt);
    private static readonly Regex TextRe = new(@"<span[^>]*class=""[^""]*voice-text[^""]*""[^>]*>(?<v>.*?)</span>", Opt);
    private static readonly Regex AriaRe = new(@"aria-label\s*=\s*""播放[:：]\s*(?<v>[^""]*)""", Opt);
    private static readonly Regex LineSplitRe = new(@"<li\b[^>]*class=""[^""]*voice-line[^""]*""", Opt);
    private static readonly Regex NoRe = new(@"data-no\s*=\s*""(?<v>\d+)""", Opt);
    private static readonly Regex MetaOgRe = new(@"<meta\b[^>]*og:title[^>]*>", Opt);

    public static string PageUrl(int unitId) => $"{Site}/friends/{unitId}/";

    public static async Task<string> FetchAsync(int unitId, CancellationToken ct)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return await Http.GetStringAsync(PageUrl(unitId), ct);
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
        throw new InvalidOperationException($"页面读取失败：{last?.Message}", last);
    }

    public static VoicePage Parse(string html, int unitId)
    {
        var styles = ParseStyles(html);
        // 声线解析不到也得留一列，否则用户无从下手
        if (styles.Count == 0)
        {
            styles.Add(new VoiceStyle(1, "语音", "默认"));
        }
        return new VoicePage(unitId, ParseCharacter(html) ?? unitId.ToString(), styles, ParseLines(html));
    }

    private static string? ParseCharacter(string html)
    {
        var meta = MetaOgRe.Match(html);
        if (!meta.Success)
        {
            return null;
        }
        var content = GetAttr(meta.Value, "content");
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }
        // og:title 形如「雾雨魔理沙 - 水子之灵的国服归言录小站」
        var cut = content.IndexOf(" - ", StringComparison.Ordinal);
        return (cut > 0 ? content[..cut] : content).Trim();
    }

    private static List<VoiceStyle> ParseStyles(string html)
    {
        var result = new List<VoiceStyle>();
        var seen = new HashSet<int>();
        foreach (Match m in ButtonRe.Matches(html))
        {
            var tag = m.Value;
            var cls = GetAttr(tag, "class");
            if (cls is null || !cls.Contains("style-btn", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (!int.TryParse(GetAttr(tag, "data-file"), out var file) || !seen.Add(file))
            {
                continue;
            }

            var title = GetAttr(tag, "title") ?? "";
            var cv = title.StartsWith("CV：", StringComparison.Ordinal) || title.StartsWith("CV:", StringComparison.Ordinal)
                ? title[3..].Trim()
                : title.Trim();

            var close = html.IndexOf("</button>", m.Index, StringComparison.OrdinalIgnoreCase);
            var body = close > m.Index ? html[m.Index..close] : tag;
            result.Add(new VoiceStyle(file, cv, Clean(FirstGroup(body, StyleNameRe) ?? "")));
        }
        result.Sort((a, b) => a.File.CompareTo(b.File));
        return result;
    }

    private static List<VoiceLine> ParseLines(string html)
    {
        var result = new List<VoiceLine>();
        // 语音可能分成多个 <ol class="voice-line-list"> 分组（1002 就是 60+6），
        // 所以只从语音区起点往后扫，不能截到第一个 </ol>
        var start = html.IndexOf("voice-line-list", StringComparison.OrdinalIgnoreCase);
        var segment = start >= 0 ? html[start..] : html;

        var parts = LineSplitRe.Split(segment);
        for (var i = 1; i < parts.Length; i++)
        {
            var body = parts[i];
            var close = body.IndexOf("</li>", StringComparison.OrdinalIgnoreCase);
            if (close >= 0)
            {
                body = body[..close];
            }

            if (!int.TryParse(FirstGroup(body, NoRe), out var no))
            {
                continue;
            }

            // 标题优先级：aria-label → voice-type-text → 空；注意 voice-play 的 title 固定是「试听」，不能用
            var title = FirstGroup(body, AriaRe) ?? FirstGroup(body, TypeTextRe) ?? "";
            var text = Clean(FirstGroup(body, TextRe) ?? "");
            result.Add(new VoiceLine(no, Clean(title), text, VoicePlayRe.IsMatch(body)));
        }

        result.Sort((a, b) => a.No.CompareTo(b.No));
        return result;
    }

    private static string Clean(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return "";
        }
        var plain = WebUtility.HtmlDecode(TagRe.Replace(raw, " "));
        return SpacesRe.Replace(plain, " ").Trim();
    }

    private static string? FirstGroup(string input, Regex re)
    {
        var m = re.Match(input);
        return m.Success ? m.Groups["v"].Value : null;
    }

    private static string? GetAttr(string tag, string name)
    {
        var m = Regex.Match(tag, name + @"\s*=\s*""(?<v>[^""]*)""", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups["v"].Value : null;
    }

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("WikiVoiceTool/1.0");
        return http;
    }
}
