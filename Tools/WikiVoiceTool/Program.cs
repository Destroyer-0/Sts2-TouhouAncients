using Aprillz.MewUI;

namespace WikiVoiceTool;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // 固定工作目录为 exe 所在目录：输入法等注入的进程会往 cwd 里丢日志，别污染用户的启动目录
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        Application.DispatcherUnhandledException += e =>
        {
            ReportFatal(e.Exception);
            e.Handled = true;
        };

        try
        {
            Win32Platform.Register();
            Direct2DBackend.Register();
            Application.Run(new MainWindow());
        }
        catch (Exception ex)
        {
            ReportFatal(ex);
        }
        finally
        {
            CleanupTemp();
        }
    }

    /// <summary>未处理异常留证：exe 同目录写日志并弹窗，避免 GUI 应用静默退出。</summary>
    private static void ReportFatal(Exception? ex)
    {
        var log = Path.Combine(AppContext.BaseDirectory, "wikivoicetool-error.log");
        try
        {
            File.WriteAllText(log, ex?.ToString() ?? "unknown error");
        }
        catch (Exception)
        {
            // 日志写不进去也必须继续弹窗
        }
        MessageBox.Notify(
            ex?.Message ?? "unknown error",
            Aprillz.MewUI.Controls.PromptIconKind.Error,
            "WikiVoiceTool 异常\n\n详情见 " + log);
    }

    /// <summary>只清试听缓存，WebView2 的 user data 目录留着（重建很慢）。</summary>
    private static void CleanupTemp()
    {
        try
        {
            var cache = Path.Combine(Path.GetTempPath(), "WikiVoiceTool", "cache");
            if (Directory.Exists(cache))
            {
                Directory.Delete(cache, recursive: true);
            }
        }
        catch (Exception)
        {
            // 清理失败无所谓
        }
    }
}
