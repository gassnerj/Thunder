using System;
using System.IO;

namespace ThunderApp.Services;

public sealed class DiskLogService : IDiskLogService
{
    private readonly string _path;

    public DiskLogService()
    {
        _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "thunder.log");
    }

    public void Log(string message)
    {
        File.AppendAllText(_path,
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
    }
}