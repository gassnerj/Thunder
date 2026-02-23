using System;
using System.IO;
using System.Text.Json;

namespace ThunderApp.Services;

public sealed class JsonSettingsService<T>(string fileName) : ISettingsService<T>
    where T : new()
{
    private readonly string _file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

    public T Load()
    {
        try
        {
            if (!File.Exists(_file))
                return new T();

            string json = File.ReadAllText(_file);

            if (string.IsNullOrWhiteSpace(json))
                return new T();

            return JsonSerializer.Deserialize<T>(json) ?? new T();
        }
        catch
        {
            // If JSON is corrupt or disk fails, don’t crash the whole app.
            return new T();
        }
    }

    public void Save(T settings)
    {
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_file, json);
    }
}