using System;
using System.Threading.Tasks;
using System.Windows;
using ThunderApp.Services;

namespace ThunderApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Log unhandled exceptions to thunder.log (if DiskLogService has been constructed).
            DispatcherUnhandledException += (_, args) =>
            {
                DiskLogService.Current?.Log("UNHANDLED UI EXCEPTION");
                DiskLogService.Current?.LogException("UI", args.Exception);
                // Let it crash normally (better than hiding issues). If you want to keep running, set Handled=true.
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                DiskLogService.Current?.Log("UNHANDLED DOMAIN EXCEPTION");
                if (args.ExceptionObject is Exception ex)
                    DiskLogService.Current?.LogException("Domain", ex);
                else
                    DiskLogService.Current?.Log("Domain exception object: " + (args.ExceptionObject?.ToString() ?? "<null>"));
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                DiskLogService.Current?.Log("UNOBSERVED TASK EXCEPTION");
                DiskLogService.Current?.LogException("TaskScheduler", args.Exception);
                args.SetObserved();
            };
        }
    }
}
