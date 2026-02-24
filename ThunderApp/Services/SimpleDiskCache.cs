using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThunderApp.Services;

public sealed class SimpleDiskCache
{
    private readonly string _dir;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SimpleDiskCache(string directoryPath)
    {
        _dir = directoryPath;
        Directory.CreateDirectory(_dir);
    }

    public async Task<string?> TryReadAsync(string key, TimeSpan maxAge)
    {
        string path = PathFor(key);
        if (!File.Exists(path)) return null;

        TimeSpan age = DateTimeOffset.UtcNow - File.GetLastWriteTimeUtc(path);
        if (age > maxAge) return null;

        try
        {
            return await File.ReadAllTextAsync(path).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task WriteAsync(string key, string value)
    {
        string path = PathFor(key);

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            // atomic-ish write
            string tmp = path + ".tmp";
            await File.WriteAllTextAsync(tmp, value).ConfigureAwait(false);
            File.Copy(tmp, path, overwrite: true);
            File.Delete(tmp);
        }
        catch
        {
            // ignore
        }
        finally
        {
            _gate.Release();
        }
    }

    private string PathFor(string key)
    {
        string file = Sha256Hex(key) + ".json";
        return Path.Combine(_dir, file);
    }

    private static string Sha256Hex(string s)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(s));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}