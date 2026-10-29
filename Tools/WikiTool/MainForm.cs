using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WikiTool;

public sealed class MainForm : Form
{
    private readonly TextBox _filter = new();
    private readonly TreeView _tree = new();
    private readonly Label _status = new();
    private readonly Button _update = new();
    private readonly Button _preview = new();
    private readonly Button _export = new();

    private readonly List<AncientRow> _rows = [];
    private readonly HashSet<string> _checked = [];
    private string? _python;
    private bool _suppressCheck;

    private static readonly string RepoRoot = FindRepoRoot();
    private static string WikiDir => Path.Combine(RepoRoot, "Tools", "wiki");
    private static string DataJs => Path.Combine(WikiDir, "data.js");
    private static string IndexHtml => Path.Combine(WikiDir, "index.html");

    public MainForm()
    {
        Text = "TouhouAncients · Wiki 更新工具";
        ClientSize = new Size(560, 720);
        MinimumSize = new Size(460, 480);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);

        var top = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8, 8, 8, 0) };
        _filter.PlaceholderText = "筛选先古之民 / 遗物…";
        _filter.Dock = DockStyle.Fill;
        _filter.TextChanged += (_, _) => BuildTree();
        var btnAll = MakeButton("全选", 62);
        var btnNone = MakeButton("全不选", 70);
        var btnInv = MakeButton("反选", 62);
        btnAll.Click += (_, _) => CheckVisible(1);
        btnNone.Click += (_, _) => CheckVisible(0);
        btnInv.Click += (_, _) => CheckVisible(2);
        var rightButtons = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false };
        rightButtons.Controls.AddRange([btnAll, btnNone, btnInv]);
        top.Controls.Add(_filter);
        top.Controls.Add(rightButtons);
        Controls.Add(top);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 96, Padding = new Padding(8) };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, WrapContents = false };
        _update.Text = "更新Wiki";
        _update.Size = new Size(110, 32);
        _update.Click += async (_, _) => await UpdateWikiAsync();
        _preview.Text = "打开预览";
        _preview.Size = new Size(110, 32);
        _preview.Click += (_, _) => OpenPreview();
        _export.Text = "导出可分享版";
        _export.Size = new Size(130, 32);
        _export.Click += (_, _) => ExportShareable();
        actions.Controls.AddRange([_update, _preview, _export]);
        _status.Dock = DockStyle.Fill;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.ForeColor = SystemColors.GrayText;
        bottom.Controls.Add(_status);
        bottom.Controls.Add(actions);
        Controls.Add(bottom);

        _tree.Dock = DockStyle.Fill;
        _tree.CheckBoxes = true;
        _tree.HideSelection = false;
        _tree.ShowLines = true;
        _tree.AfterCheck += OnAfterCheck;
        Controls.Add(_tree);
        _tree.BringToFront();

        Load += async (_, _) => await LoadListingAsync();
    }

    private static Button MakeButton(string text, int width) =>
        new() { Text = text, Width = width, Height = 28, Margin = new Padding(6, 0, 0, 0) };

    // exe 可能在 Tools/WikiTool/ 也可能在 bin/Release/... 下，逐级向上找标记文件定位仓库根
    private static string FindRepoRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Tools", "build_wiki.py")))
                {
                    return dir.FullName;
                }
            }
        }
        return AppContext.BaseDirectory;
    }

    private async Task LoadListingAsync()
    {
        if (!File.Exists(Path.Combine(RepoRoot, "Tools", "build_wiki.py")))
        {
            _status.Text = "找不到仓库根（Tools/build_wiki.py）";
            SetEnabled(false);
            return;
        }

        _status.Text = "正在读取清单…";
        SetEnabled(false);
        try
        {
            var (code, stdout, stderr) = await RunPythonAsync("--list");
            if (code != 0)
            {
                ShowError("读取清单失败", stderr);
                _status.Text = "读取清单失败";
                return;
            }
            ParseListing(stdout);
            _checked.Clear();
            foreach (var row in _rows)
            {
                foreach (var relic in row.Relics)
                {
                    _checked.Add(relic.Id);
                }
            }
            BuildTree();
            RefreshStatus();
        }
        catch (Exception ex)
        {
            ShowError("读取清单失败", ex.Message);
            _status.Text = "读取清单失败";
        }
        finally
        {
            SetEnabled(true);
        }
    }

    private void ParseListing(string json)
    {
        _rows.Clear();
        using var doc = JsonDocument.Parse(json);
        foreach (var a in doc.RootElement.EnumerateArray())
        {
            var row = new AncientRow
            {
                Id = a.GetProperty("id").GetString() ?? "",
                Name = a.GetProperty("name").GetString() ?? "",
                Epithet = a.TryGetProperty("epithet", out var e) ? e.GetString() ?? "" : "",
                Tier = a.TryGetProperty("showActLabel", out var t) ? t.GetString() ?? "" : "",
            };
            if (a.TryGetProperty("relics", out var relics))
            {
                foreach (var r in relics.EnumerateArray())
                {
                    row.Relics.Add(new RelicRow
                    {
                        Id = r.GetProperty("id").GetString() ?? "",
                        Name = r.GetProperty("name").GetString() ?? "",
                    });
                }
            }
            _rows.Add(row);
        }
    }

    private void BuildTree()
    {
        var q = _filter.Text.Trim();
        _suppressCheck = true;
        _tree.BeginUpdate();
        _tree.Nodes.Clear();
        foreach (var row in _rows)
        {
            var hitRelics = q.Length == 0
                ? row.Relics
                : row.Relics.Where(r => r.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            var selfHit = row.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                          || row.Epithet.Contains(q, StringComparison.OrdinalIgnoreCase);
            if (q.Length > 0 && !selfHit && hitRelics.Count == 0)
            {
                continue;
            }
            var label = string.IsNullOrEmpty(row.Epithet) ? row.Name : $"{row.Name}  —  {row.Epithet}";
            var parent = new TreeNode($"{label}　[{row.Tier}]") { Tag = row.Id };
            foreach (var relic in hitRelics)
            {
                parent.Nodes.Add(new TreeNode(relic.Name) { Tag = relic.Id });
            }
            _tree.Nodes.Add(parent);
        }
        // 勾选状态先同步到节点，再决定父节点显示（父勾 = 全部子勾）
        foreach (TreeNode parent in _tree.Nodes)
        {
            var all = true;
            foreach (TreeNode child in parent.Nodes)
            {
                child.Checked = _checked.Contains((string)child.Tag);
                all &= child.Checked;
            }
            parent.Checked = parent.Nodes.Count > 0 && all;
        }
        _tree.EndUpdate();
        _suppressCheck = false;
        RefreshStatus();
    }

    private int AncientsWithAnyChecked() => _rows.Count(r => r.Relics.Any(x => _checked.Contains(x.Id)));

    private void OnAfterCheck(object? sender, TreeViewEventArgs e)
    {
        if (_suppressCheck)
        {
            return;
        }
        _suppressCheck = true;
        try
        {
            if (e.Node?.Parent == null)
            {
                foreach (TreeNode child in e.Node!.Nodes)
                {
                    child.Checked = e.Node.Checked;
                    SetRelic((string)child.Tag, child.Checked);
                }
                if (e.Node.Checked && e.Node.Nodes.Count == 0)
                {
                    // 空先古（无遗物）：无子节点可勾，忽略
                    e.Node.Checked = false;
                }
            }
            else
            {
                SetRelic((string)e.Node.Tag, e.Node.Checked);
                var parent = e.Node.Parent!;
                parent.Checked = parent.Nodes.Count > 0
                                 && parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
            }
        }
        finally
        {
            _suppressCheck = false;
        }
        RefreshStatus();
    }

    private void SetRelic(string id, bool value)
    {
        if (value)
        {
            _checked.Add(id);
        }
        else
        {
            _checked.Remove(id);
        }
    }

    private void CheckVisible(int mode)
    {
        foreach (TreeNode parent in _tree.Nodes)
        {
            foreach (TreeNode child in parent.Nodes)
            {
                var id = (string)child.Tag;
                var next = mode switch
                {
                    0 => false,
                    1 => true,
                    _ => !_checked.Contains(id),
                };
                child.Checked = next;
                SetRelic(id, next);
            }
            parent.Checked = parent.Nodes.Count > 0 && parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
        }
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var selectedAncients = AncientsWithAnyChecked();
        var last = ReadGeneratedAt() ?? "未知";
        _status.Text = $"已勾选 {selectedAncients} 位 / {_checked.Count} 件（共 {_rows.Count} 位 · {_rows.Sum(r => r.Relics.Count)} 件）"
                       + $" · 上次更新 {last}";
    }

    private static string? ReadGeneratedAt()
    {
        try
        {
            if (!File.Exists(DataJs))
            {
                return null;
            }
            var text = File.ReadAllText(DataJs, Encoding.UTF8);
            var m = Regex.Match(text, "\"generatedAt\"\\s*:\\s*\"([^\"]+)\"");
            return m.Success ? m.Groups[1].Value : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private async Task UpdateWikiAsync()
    {
        var ancients = _rows.Where(r => r.Relics.Any(x => _checked.Contains(x.Id))).Select(r => r.Id).ToList();
        var relics = _rows.SelectMany(r => r.Relics).Where(x => _checked.Contains(x.Id)).Select(x => x.Id).ToList();

        SetEnabled(false);
        _status.Text = "正在生成 data.js…";
        // 选择结果写在 exe 同目录：%TEMP% 在受限环境里可能不可写
        var selectFile = Path.Combine(AppContext.BaseDirectory, "select.json");
        try
        {
            await File.WriteAllTextAsync(selectFile,
                JsonSerializer.Serialize(new { ancients, relics }), Encoding.UTF8);
            var (code, stdout, stderr) = await RunPythonAsync($"--select \"{selectFile}\"");
            if (code != 0)
            {
                ShowError("更新Wiki 失败", string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
                return;
            }
            _status.Text = stdout.Trim();
            RefreshStatus();
        }
        catch (Exception ex)
        {
            ShowError("更新Wiki 失败", ex.Message);
        }
        finally
        {
            try
            {
                File.Delete(selectFile);
            }
            catch (IOException)
            {
                // 临时文件删不掉不影响结果
            }
            SetEnabled(true);
        }
    }

    private void OpenPreview()
    {
        if (!File.Exists(IndexHtml))
        {
            ShowError("打开预览失败", $"找不到 {IndexHtml}");
            return;
        }
        Process.Start(new ProcessStartInfo(IndexHtml) { UseShellExecute = true });
    }

    private void ExportShareable()
    {
        var dist = Path.Combine(WikiDir, "dist");
        var zip = Path.Combine(WikiDir, "dist.zip");
        if (Directory.Exists(dist) &&
            MessageBox.Show(this, $"{dist}\n已存在，覆盖吗？", "导出可分享版",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
        {
            return;
        }

        try
        {
            SetEnabled(false);
            if (Directory.Exists(dist))
            {
                Directory.Delete(dist, recursive: true);
            }
            Directory.CreateDirectory(dist);
            foreach (var name in new[] { "index.html", "style.css", "app.js" })
            {
                File.Copy(Path.Combine(WikiDir, name), Path.Combine(dist, name), overwrite: true);
            }

            // data.js 里的图片是 ../../images/，导出后要落到 dist/images/ 并改写 assetPrefix
            var dataFiles = new[] { "data.js", "data.en.js", "data.jpn.js" }
                .Where(n => File.Exists(Path.Combine(WikiDir, n)))
                .ToList();
            var data = string.Join("\n", dataFiles.Select(n => File.ReadAllText(Path.Combine(WikiDir, n), Encoding.UTF8)));
            var assets = Regex.Matches(data, "\\.\\./\\.\\./images/[^\"]+")
                              .Select(m => m.Value)
                              .Distinct()
                              .ToList();
            var missing = new List<string>();
            foreach (var asset in assets)
            {
                var rel = asset.Replace("../../", "").Replace('/', Path.DirectorySeparatorChar);
                var source = Path.Combine(RepoRoot, rel);
                var target = Path.Combine(dist, rel);
                if (!File.Exists(source))
                {
                    missing.Add(asset);
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(source, target, overwrite: true);
            }
            // 每个语言的 data 都要把内嵌的 ../../images/ 改成 dist 内的 images/
            foreach (var name in dataFiles)
            {
                var exported = File.ReadAllText(Path.Combine(WikiDir, name), Encoding.UTF8)
                                   .Replace("../../images/", "images/");
                File.WriteAllText(Path.Combine(dist, name), exported, new UTF8Encoding(false));
            }

            if (File.Exists(zip))
            {
                File.Delete(zip);
            }
            ZipFile.CreateFromDirectory(dist, zip, CompressionLevel.Optimal, includeBaseDirectory: false);

            var message = $"已导出 {assets.Count - missing.Count} 张图片\n目录：{dist}\n压缩包：{zip}";
            if (missing.Count > 0)
            {
                message += $"\n\n缺失 {missing.Count} 张（未复制）：\n" + string.Join("\n", missing.Take(10));
            }
            MessageBox.Show(this, message, "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{zip}\""));
        }
        catch (Exception ex)
        {
            ShowError("导出可分享版失败", ex.Message);
        }
        finally
        {
            SetEnabled(true);
        }
    }

    private void SetEnabled(bool enabled)
    {
        UseWaitCursor = !enabled;
        _update.Enabled = enabled;
        _export.Enabled = enabled;
        _preview.Enabled = enabled;
        _filter.Enabled = enabled;
        _tree.Enabled = enabled;
    }

    private void ShowError(string title, string detail) =>
        MessageBox.Show(this, detail, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

    private async Task<(int Code, string StdOut, string StdErr)> RunPythonAsync(string arguments)
    {
        _python ??= await DetectPythonAsync();
        var (exe, prefix) = _python == "py -3" ? ("py", "-3 ") : (_python!, "");
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"{prefix}\"Tools\\build_wiki.py\" {arguments}",
            WorkingDirectory = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        // 否则中文输出会按控制台代码页编码，exe 读成乱码
        psi.Environment["PYTHONUTF8"] = "1";
        psi.Environment["PYTHONIOENCODING"] = "utf-8";

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("无法启动 Python。");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await stdout, await stderr);
    }

    private static async Task<string> DetectPythonAsync()
    {
        foreach (var candidate in new[] { "py -3", "python" })
        {
            var (exe, args) = candidate == "py -3" ? ("py", "-3 --version") : ("python", "--version");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var process = Process.Start(psi);
                if (process == null)
                {
                    continue;
                }
                await process.WaitForExitAsync();
                if (process.ExitCode == 0)
                {
                    return candidate;
                }
            }
            catch (Exception)
            {
                // 该候选不存在，试下一个
            }
        }
        throw new InvalidOperationException("未找到 Python，请安装 Python 3 并加入 PATH（或 py 启动器）。");
    }

    private sealed class AncientRow
    {
        public string Id = "";
        public string Name = "";
        public string Epithet = "";
        public string Tier = "";
        public List<RelicRow> Relics { get; } = [];
    }

    private sealed class RelicRow
    {
        public string Id = "";
        public string Name = "";
    }
}
