using System;
using System.IO;
using System.Text.Json;
using ThunderApp.Models;

namespace ThunderApp.Services;

public static class AppSettingsLoader
{
    public static AppSettings Load()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(path)) return new AppSettings();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}
