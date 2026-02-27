using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace ThunderApp.Services;

public sealed class DiskLogService : IDiskLogService
{
    private static readonly object _gate = new();

    // Keep a process-wide reference so we can log unhandled exceptions early.
    public static DiskLogService? Current { get; private set; }

    private readonly string _path;
    private readonly long _rollBytes;

    public DiskLogService(string? fileName = null, long rollBytes = 5 * 1024 * 1024)
    {
        _rollBytes = rollBytes;
        _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName ?? "thunder.log");
        Current = this;

        // Mark session start.
        Log("=== Session start ===");
    }

    public void Log(string message)
    {
        if (message is null) return;

        var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        int tid = Environment.CurrentManagedThreadId;

        // Keep log lines single-line friendly.
        string safe = message.Replace("\r", " ").Replace("\n", " ");

        string line = $"[{ts}] [T{tid}] {safe}{Environment.NewLine}";

        lock (_gate)
        {
            try
            {
                RollIfNeeded_NoThrow();
                File.AppendAllText(_path, line);
            }
            catch
            {
                // Never throw from logging.
            }
        }
    }

    public void LogException(string context, Exception ex)
    {
        if (ex == null)
        {
            Log($"{context}: <null exception>");
            return;
        }

        Log($"{context}: {ex.GetType().Name}: {ex.Message}");
        Log(ex.StackTrace ?? "<no stack>");
        if (ex.InnerException != null)
        {
            Log($"Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            Log(ex.InnerException.StackTrace ?? "<no inner stack>");
        }
    }

    private void RollIfNeeded_NoThrow()
    {
        try
        {
            var fi = new FileInfo(_path);
            if (!fi.Exists) return;
            if (fi.Length < _rollBytes) return;

            string dir = fi.DirectoryName ?? AppDomain.CurrentDomain.BaseDirectory;
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string archived = Path.Combine(dir, $"thunder_{stamp}.log");

            File.Move(_path, archived, overwrite: true);
        }
        catch
        {
            // ignore
        }
    }
}
