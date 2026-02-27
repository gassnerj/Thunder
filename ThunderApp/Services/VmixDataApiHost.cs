using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ThunderApp.Models;

namespace ThunderApp.Services;

public sealed class VmixDataApiHost : IDisposable
{
    private readonly string _prefix;
    private readonly Func<VmixApiSnapshot> _snapshotProvider;
    private readonly HttpListener _listener = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public VmixDataApiHost(string prefix, Func<VmixApiSnapshot> snapshotProvider)
    {
        _prefix = EnsureTrailingSlash(prefix);
        _snapshotProvider = snapshotProvider;
    }

    public string Prefix => _prefix;

    public void Start()
    {
        if (_cts != null) return;

        _listener.Prefixes.Clear();
        _listener.Prefixes.Add(_prefix);
        _listener.Start();

        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }

        try
        {
            if (_listener.IsListening)
                _listener.Stop();
        }
        catch { }

        _cts?.Dispose();
        _cts = null;
        _loop = null;
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext? ctx = null;
            try
            {
                ctx = await _listener.GetContextAsync();
                await HandleAsync(ctx);
            }
            catch (HttpListenerException)
            {
                if (ct.IsCancellationRequested) break;
            }
            catch (ObjectDisposedException)
            {
                if (ct.IsCancellationRequested) break;
            }
            catch
            {
                if (ctx != null)
                {
                    try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
                }
            }
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        string path = (ctx.Request.Url?.AbsolutePath ?? "/").TrimEnd('/').ToLowerInvariant();
        if (path.Length == 0) path = "/";

        var snapshot = _snapshotProvider();

        object payload = path switch
        {
            "/" => new
            {
                ok = true,
                service = "Thunder vMix Data API",
                endpoints = new[]
                {
                    "/api/v1/vmix/health",
                    "/api/v1/vmix/snapshot",
                    "/api/v1/vmix/table",
                    "/api/v1/vmix/warnings",
                    "/api/v1/vmix/warnings/table",
                    "/api/v1/vmix/observation"
                },
                snapshot.generatedAtUtc
            },
            "/api/v1/vmix/health" => new { ok = true, snapshot.generatedAtUtc, snapshot.sourceRequested, snapshot.sourceActive },
            "/api/v1/vmix/warnings" => new { snapshot.generatedAtUtc, snapshot.radiusMiles, snapshot.useRadiusFilter, warnings = snapshot.warnings, rows = EnsureWarningsTable(snapshot) },
            "/api/v1/vmix/warnings/table" => EnsureWarningsTable(snapshot),
            "/api/v1/vmix/observation" => new { snapshot.generatedAtUtc, snapshot.sourceRequested, snapshot.sourceActive, snapshot.vehicleStationAvailable, observation = snapshot.observation, rows = snapshot.rows },
            "/api/v1/vmix/snapshot" => snapshot,
            "/api/v1/vmix/table" => snapshot.rows,
            _ => new { ok = false, error = "not_found" }
        };

        int status = path is "/" or "/api/v1/vmix/health" or "/api/v1/vmix/warnings" or "/api/v1/vmix/warnings/table" or "/api/v1/vmix/observation" or "/api/v1/vmix/snapshot" or "/api/v1/vmix/table"
            ? 200 : 404;

        string json = JsonSerializer.Serialize(payload, _jsonOptions);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        ctx.Response.ContentEncoding = Encoding.UTF8;
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        ctx.Response.Close();
    }

    private static IReadOnlyList<VmixWarningDto> EnsureWarningsTable(VmixApiSnapshot snapshot)
    {
        if (snapshot.warnings is { Count: > 0 })
            return snapshot.warnings;

        return new[]
        {
            new VmixWarningDto
            {
                Id = "",
                Event = "",
                Headline = "",
                Severity = "",
                Urgency = "",
                AreaDescription = ""
            }
        };
    }

    private static string EnsureTrailingSlash(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return "http://127.0.0.1:8787/";
        return prefix.EndsWith('/') ? prefix : prefix + "/";
    }

    public void Dispose() => Stop();
}
