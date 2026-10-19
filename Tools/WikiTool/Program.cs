namespace WikiTool;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // 固定工作目录为 exe 所在目录：输入法等注入的进程会往 cwd 里丢日志，别污染用户的启动目录
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        Application.ThreadException += (_, e) => ReportFatal(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ReportFatal(e.ExceptionObject as Exception);
        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            ReportFatal(ex);
        }
    }

    /// <summary>未处理异常留证：exe 同目录写日志并弹窗，避免 GUI 应用静默退出。</summary>
    private static void ReportFatal(Exception? ex)
    {
        var log = Path.Combine(AppContext.BaseDirectory, "wikitool-error.log");
        try
        {
            File.WriteAllText(log, ex?.ToString() ?? "unknown error");
        }
        catch (Exception)
        {
            // 日志写不进去也必须继续弹窗
        }
        MessageBox.Show((ex?.Message ?? "unknown error") + "\n\n详情见 " + log, "WikiTool 异常",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
