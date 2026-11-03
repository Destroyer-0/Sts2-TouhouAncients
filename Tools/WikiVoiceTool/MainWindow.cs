using System.Text.RegularExpressions;
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using WikiVoiceTool.Models;
using WikiVoiceTool.Services;

namespace WikiVoiceTool;

public sealed class MainWindow : Window
{
    private sealed record CellContext(VoiceLine Line, VoiceStyle Style, bool Play);

    private readonly TextBox _url = new();
    private readonly TextBox _outDir = new();
    private readonly Button _find = new() { Content = new TextBlock { Text = "查找" }, Width = 84, Height = 30 };
    private readonly Button _browse = new() { Content = new TextBlock { Text = "浏览…" }, Width = 84, Height = 30 };
    private readonly Button _dlStyle = new() { Content = new TextBlock { Text = "全部下载(当前声线)" }, Width = 160, Height = 30 };
    private readonly Button _dlAll = new() { Content = new TextBlock { Text = "全部下载(所有声线)" }, Width = 160, Height = 30 };
    private readonly Button _cancel = new() { Content = new TextBlock { Text = "取消" }, Width = 84, Height = 30 };
    private readonly ComboBox _styleBox = new() { Width = 200, Height = 30 };
    private readonly ProgressBar _progress = new() { Width = 180, Height = 18, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _status = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly Border _host = new();
    private readonly AudioPlayer _audio = new();

    private readonly Dictionary<(int No, int Style), TextBlock> _dlLabels = [];
    private static readonly Regex UnitIdRe = new(@"(?:friends/)?(\d{2,})", RegexOptions.IgnoreCase);

    private VoicePage? _page;
    private CancellationTokenSource? _cts;
    private (int No, int Style)? _playing;

    public MainWindow()
    {
        Title = "WikiVoiceTool · 角色语音抓取器";
        WindowSize = Aprillz.MewUI.WindowSize.Resizable(
            1120, 680, 900, 520, double.PositiveInfinity, double.PositiveInfinity);
        StartupLocation = WindowStartupLocation.CenterScreen;
        Padding = new Thickness(12);
        Content = BuildRoot();
        Closed += () =>
        {
            CancelJobs();
            _audio.Dispose();
        };
    }

    // ---- UI ----

    private Element BuildRoot()
    {
        var top = new Grid { Spacing = 8 };
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        top.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        top.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _url.Placeholder = "https://eikaebisu.moe/friends/1002/ 或直接填 1002";
        _url.Height = 30;
        _url.Margin = new Thickness(0, 2);

        _find.Click += () => _ = FindAsync();

        _outDir.Height = 30;
        _outDir.Margin = new Thickness(0, 2);
        _outDir.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WikiVoiceTool");

        _browse.Click += () => BrowseOutDir();

        AddToGrid(top, Caption("角色页网址"), 0, 0);
        AddToGrid(top, _url, 0, 1);
        AddToGrid(top, _find, 0, 2);
        AddToGrid(top, Caption("输出路径"), 1, 0);
        AddToGrid(top, _outDir, 1, 1);
        AddToGrid(top, _browse, 1, 2);

        _host.HorizontalAlignment = HorizontalAlignment.Stretch;
        _host.VerticalAlignment = VerticalAlignment.Stretch;

        _styleBox.Margin = new Thickness(0, 2);
        _dlStyle.Click += () => _ = DownloadCurrentStyleAsync();
        _dlAll.Click += () => _ = DownloadBatchAsync(null);
        _cancel.IsEnabled = false;
        _cancel.Click += () =>
        {
            _cts?.Cancel();
            _audio.Stop();
            SetStatus("正在取消…");
        };

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Add(_styleBox);
        actions.Add(_dlStyle);
        actions.Add(_dlAll);
        actions.Add(_cancel);
        actions.Add(_progress);

        var bottom = new StackPanel { Spacing = 6 };
        bottom.Add(actions);
        bottom.Add(_status);

        var root = new Grid { Spacing = 8 };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddToGrid(root, top, 0, 0);
        AddToGrid(root, _host, 1, 0);
        AddToGrid(root, bottom, 2, 0);

        SetStatus("就绪");
        SetBusy(false);
        return root;
    }

    private static TextBlock Caption(string text) => new()
    {
        Text = text,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static void AddToGrid(Grid grid, Element element, int row, int column)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        grid.Add(element);
    }

    private void BrowseOutDir()
    {
        var folder = FileDialog.SelectFolder(new FolderDialogOptions
        {
            Owner = this,
            Title = "选择语音输出目录",
        });
        if (!string.IsNullOrWhiteSpace(folder))
        {
            _outDir.Text = folder;
        }
    }

    // ---- 查找 ----

    private static int? ParseUnitId(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }
        var m = UnitIdRe.Match(input.Trim());
        return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : null;
    }

    private async Task FindAsync()
    {
        var unitId = ParseUnitId(_url.Text);
        if (unitId is null)
        {
            SetStatus("网址里找不到角色 id");
            return;
        }
        if (string.IsNullOrWhiteSpace(_outDir.Text))
        {
            SetStatus("请先指定输出路径");
            return;
        }

        CancelJobs();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        SetBusy(true);
        SetStatus("正在读取页面…");
        try
        {
            var html = await EikaPageParser.FetchAsync(unitId.Value, ct);
            var page = EikaPageParser.Parse(html, unitId.Value);
            if (page.Lines.Count == 0)
            {
                SetStatus("该页面不是角色页（没有语音行）");
                return;
            }

            _page = page;
            _playing = null;
            _audio.Stop();
            TryWriteManifest(page);
            ShowPage(page);

            var withAudio = page.Lines.Count(l => l.HasAudio);
            SetStatus($"{page.CharacterName}：{page.Lines.Count} 条语音，{withAudio} 条可下载，{page.Styles.Count} 套声线");
        }
        catch (OperationCanceledException)
        {
            SetStatus("已取消");
        }
        catch (Exception ex)
        {
            SetStatus("读取失败：" + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void TryWriteManifest(VoicePage page)
    {
        try
        {
            VoiceDownloader.WriteManifest(page, _outDir.Text);
        }
        catch (UnauthorizedAccessException)
        {
            SetStatus("输出目录无写权限，请换一个目录");
        }
        catch (Exception ex)
        {
            SetStatus("manifest 写入失败：" + ex.Message);
        }
    }

    // ---- 表格 ----

    private void ShowPage(VoicePage page)
    {
        _dlLabels.Clear();
        _styleBox.ItemsSource = ItemsView.Create(
            page.Styles,
            s => string.IsNullOrWhiteSpace(s.Cv) ? s.DisplayName : s.Cv,
            s => s.File);
        _styleBox.SelectedIndex = 0;
        _host.Child = BuildGrid(page);
    }

    private GridView BuildGrid(VoicePage page)
    {
        var columns = new List<GridViewColumn<VoiceLine>>
        {
            new()
            {
                Header = "编号",
                Width = new GridLength(56, GridUnitType.Pixel),
                CellTemplate = new TextCellTemplate(l => l.No.ToString()),
            },
            new()
            {
                Header = "标题",
                Width = new GridLength(140, GridUnitType.Pixel),
                CellTemplate = new TextCellTemplate(l => string.IsNullOrWhiteSpace(l.Title) ? "（未命名）" : l.Title),
            },
            new()
            {
                Header = "台词",
                Width = GridLength.Star,
                CellTemplate = new TextCellTemplate(l => l.Text),
            },
        };

        foreach (var style in page.Styles)
        {
            var label = string.IsNullOrWhiteSpace(style.Cv) ? style.DisplayName : style.Cv;
            if (string.IsNullOrWhiteSpace(label))
            {
                label = $"声线{style.File}";
            }

            columns.Add(new GridViewColumn<VoiceLine>
            {
                Header = $"{label} 试听",
                Width = new GridLength(104, GridUnitType.Pixel),
                CellTemplate = new CellButtonTemplate(this, style, play: true),
            });
            columns.Add(new GridViewColumn<VoiceLine>
            {
                Header = $"{label} 下载",
                Width = new GridLength(104, GridUnitType.Pixel),
                CellTemplate = new CellButtonTemplate(this, style, play: false),
            });
        }

        var grid = new GridView
        {
            ShowGridLines = true,
            ZebraStriping = false,
            RowHeight = 34,
        };
        grid.SetColumns(columns);
        grid.ItemsSource = ItemsView.Create(page.Lines, l => l.Title, l => l.No);
        return grid;
    }

    private void BindCell(Button button, VoiceLine line, VoiceStyle style, bool play)
    {
        if (button.Tag is CellContext old && !old.Play)
        {
            _dlLabels.Remove((old.Line.No, old.Style.File));
        }
        if (button.Content is not TextBlock label)
        {
            return;
        }

        label.Text = line.HasAudio ? (play ? "▶" : "↓") : "—";
        button.IsEnabled = line.HasAudio;
        if (!line.HasAudio)
        {
            button.Tag = null;
            return;
        }

        button.Tag = new CellContext(line, style, play);
        if (!play)
        {
            _dlLabels[(line.No, style.File)] = label;
        }
    }

    private async void OnCellClicked(Button button)
    {
        if (_page is null || button.Tag is not CellContext ctx)
        {
            return;
        }
        try
        {
            if (ctx.Play)
            {
                await TogglePlayAsync(ctx.Line, ctx.Style);
            }
            else
            {
                await DownloadOneAsync(ctx.Line, ctx.Style);
            }
        }
        catch (Exception ex)
        {
            SetStatus("操作失败：" + ex.Message);
        }
    }

    /// <summary>单元格模板：文本列。</summary>
    private sealed class TextCellTemplate(Func<VoiceLine, string> selector) : IDataTemplate<VoiceLine>
    {
        public FrameworkElement Build(TemplateContext context) => new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 4),
        };

        public void Bind(FrameworkElement view, VoiceLine item, int index, TemplateContext context)
            => ((TextBlock)view).Text = selector(item);

        public void Unbind(FrameworkElement view, VoiceLine item, int index, TemplateContext context)
        {
        }

        public void Bind(FrameworkElement view, object? item, int index, TemplateContext context)
            => Bind(view, (VoiceLine)item!, index, context);

        public void Unbind(FrameworkElement view, object? item, int index, TemplateContext context)
        {
        }
    }

    /// <summary>单元格模板：试听/下载按钮。单元格会回收复用，数据存在 Tag 里。</summary>
    private sealed class CellButtonTemplate(MainWindow owner, VoiceStyle style, bool play) : IDataTemplate<VoiceLine>
    {
        public FrameworkElement Build(TemplateContext context)
        {
            var label = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var button = new Button
            {
                Content = label,
                Padding = new Thickness(4, 1),
                Height = 24,
            };
            button.Click += () => owner.OnCellClicked(button);
            return button;
        }

        public void Bind(FrameworkElement view, VoiceLine item, int index, TemplateContext context)
            => owner.BindCell((Button)view, item, style, play);

        public void Unbind(FrameworkElement view, VoiceLine item, int index, TemplateContext context)
        {
        }

        public void Bind(FrameworkElement view, object? item, int index, TemplateContext context)
            => Bind(view, (VoiceLine)item!, index, context);

        public void Unbind(FrameworkElement view, object? item, int index, TemplateContext context)
        {
        }
    }

    // ---- 试听 ----

    private async Task TogglePlayAsync(VoiceLine line, VoiceStyle style)
    {
        if (_page is null)
        {
            return;
        }
        var key = (line.No, style.File);
        if (_playing == key)
        {
            _audio.Stop();
            _playing = null;
            SetStatus("已停止试听");
            return;
        }

        _playing = key;
        SetStatus($"正在准备试听：{line.No:00} {line.Title}（{style.Cv}）…");
        var ok = await _audio.PlayAsync(_page.UnitId, style.File, line.No, CancellationToken.None);
        if (!ok)
        {
            _playing = null;
            SetStatus("试听失败：无法解码该音频");
            return;
        }
        SetStatus($"试听中：{line.No:00} {line.Title}（{style.Cv}）");
    }

    // ---- 下载 ----

    private async Task DownloadOneAsync(VoiceLine line, VoiceStyle style)
    {
        if (_page is null)
        {
            return;
        }
        var path = VoiceDownloader.BuildPath(_outDir.Text, _page.UnitId, line, style);
        var url = VoiceDownloader.BuildUrl(_page.UnitId, style.File, line.No);
        SetStatus($"正在下载 {line.No:00} {line.Title}（{style.Cv}）…");

        var result = await VoiceDownloader.DownloadAsync(url, path, CancellationToken.None);
        if (result == DownloadResult.Failed)
        {
            SetStatus($"下载失败：{line.No:00} {line.Title}（{style.Cv}）");
            return;
        }

        MarkDownloaded(line.No, style.File);
        SetStatus($"{(result == DownloadResult.Skipped ? "已存在" : "已保存")}：{Path.GetFileName(path)}");
    }

    private void MarkDownloaded(int no, int styleFile)
    {
        if (_dlLabels.TryGetValue((no, styleFile), out var label))
        {
            label.Text = "✔";
        }
    }

    private async Task DownloadCurrentStyleAsync()
    {
        if (_page is null)
        {
            SetStatus("先点「查找」");
            return;
        }
        var index = _styleBox.SelectedIndex;
        var styleFile = index >= 0 && index < _page.Styles.Count
            ? _page.Styles[index].File
            : _page.Styles[0].File;
        await DownloadBatchAsync(styleFile);
    }

    private async Task DownloadBatchAsync(int? styleFile)
    {
        if (_page is null)
        {
            SetStatus("先点「查找」");
            return;
        }

        var page = _page;
        var styles = styleFile is null ? page.Styles : page.Styles.Where(s => s.File == styleFile).ToList();
        var jobs = new List<(VoiceLine Line, VoiceStyle Style)>();
        foreach (var style in styles)
        {
            foreach (var line in page.Lines)
            {
                if (line.HasAudio)
                {
                    jobs.Add((line, style));
                }
            }
        }
        if (jobs.Count == 0)
        {
            SetStatus("没有可下载的语音");
            return;
        }

        CancelJobs();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        SetBusy(true);
        _progress.Maximum = jobs.Count;
        _progress.Value = 0;
        SetStatus($"开始下载 0 / {jobs.Count}");

        var done = 0;
        var failed = 0;
        using var gate = new SemaphoreSlim(4);
        try
        {
            var tasks = jobs.Select(async job =>
            {
                await gate.WaitAsync(ct);
                try
                {
                    var path = VoiceDownloader.BuildPath(_outDir.Text, page.UnitId, job.Line, job.Style);
                    var url = VoiceDownloader.BuildUrl(page.UnitId, job.Style.File, job.Line.No);
                    var result = await VoiceDownloader.DownloadAsync(url, path, ct);
                    var n = Interlocked.Increment(ref done);
                    if (result == DownloadResult.Failed)
                    {
                        Interlocked.Increment(ref failed);
                    }
                    else
                    {
                        // 单元格映射只在 UI 线程改，后台线程必须转过去
                        Ui(() => MarkDownloaded(job.Line.No, job.Style.File));
                    }
                    Ui(() => _progress.Value = Math.Min(n, jobs.Count));
                    SetStatus($"已下载 {n} / {jobs.Count}，失败 {failed}");
                }
                catch (OperationCanceledException)
                {
                    // 取消时静默退出
                }
                finally
                {
                    gate.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks);
            SetStatus($"完成：共 {jobs.Count} 条，失败 {failed}");
        }
        catch (OperationCanceledException)
        {
            SetStatus($"已取消（完成 {done} / {jobs.Count}，失败 {failed}）");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ---- 状态 ----

    private void SetStatus(string text) => Ui(() => _status.Text = text);

    private void SetBusy(bool busy) => Ui(() =>
    {
        _find.IsEnabled = !busy;
        _browse.IsEnabled = !busy;
        _dlStyle.IsEnabled = !busy && _page is not null;
        _dlAll.IsEnabled = !busy && _page is not null;
        _cancel.IsEnabled = busy;
    });

    private void CancelJobs()
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        catch (Exception)
        {
            // 已释放
        }
        _cts = null;
    }

    /// <summary>后台任务里更新界面：MewUI 控件只允许在 UI 线程修改。</summary>
    private static void Ui(Action action)
    {
        var dispatcher = Application.IsRunning ? Application.Current.Dispatcher : null;
        if (dispatcher is null || dispatcher.IsOnUIThread)
        {
            action();
        }
        else
        {
            dispatcher.BeginInvoke(action);
        }
    }
}

