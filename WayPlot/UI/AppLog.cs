using WayPlot.domain.Common;

namespace WayPlot.UI;

/// <summary>
/// Tiny file logger so swallowed exceptions and slow screen loads leave a trace.
/// Writes to %LocalAppData%\WayPlot\logs\wayplot.log. Never throws.
/// </summary>
public static class AppLog
{
    private static readonly object Gate = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WayPlot", "logs", "wayplot.log");

    public static void Info(string message) => Write("INFO ", message);

    public static void Error(string where, Exception ex) =>
        Write("ERROR", $"{where}: {SensitiveValueRedactor.Redact(ex.ToString())}");

    private static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {level} [{Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never break the app.
        }
    }
}
