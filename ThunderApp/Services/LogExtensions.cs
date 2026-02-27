using System;

namespace ThunderApp.Services;

public static class LogExtensions
{
    public static void Info(this IDiskLogService log, string message) => log.Log("[INFO] " + message);
    public static void Warn(this IDiskLogService log, string message) => log.Log("[WARN] " + message);
    public static void Error(this IDiskLogService log, string message) => log.Log("[ERR ] " + message);

    public static void Error(this IDiskLogService log, string context, Exception ex)
    {
        try
        {
            log.Log("[ERR ] " + context + ": " + ex.GetType().Name + ": " + ex.Message);
            log.Log(ex.StackTrace ?? "<no stack>");
            if (ex.InnerException != null)
            {
                log.Log("[ERR ] Inner: " + ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
                log.Log(ex.InnerException.StackTrace ?? "<no inner stack>");
            }
        }
        catch
        {
            // don't throw from logging
        }
    }
}
