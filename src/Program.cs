using System;
using System.Windows;
using CodexUsageViewer.Usage;

namespace CodexUsageViewer
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                string instanceSuffix = args != null && Array.IndexOf(args, "--isolated-test") >= 0 ? ".isolated." + System.Diagnostics.Process.GetCurrentProcess().Id : string.Empty;
                using (SingleInstance singleInstance = new SingleInstance(instanceSuffix))
                {
                    if (!singleInstance.IsPrimary) { singleInstance.SignalExisting(); return; }
                    AppLogger.Info("Application starting v" + ProgramInfo.Version);
                    try { ProgramNetworkAudit.Write(); } catch (Exception exception) { AppLogger.Error("Program network audit write failed", exception); }

                    Application application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    application.DispatcherUnhandledException += delegate(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) { AppLogger.Error("Unhandled UI exception", e.Exception); e.Handled = true; };
                    AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { AppLogger.Error("Unhandled application exception", e.ExceptionObject as Exception); };
                    MainWindow window = new MainWindow(new UsageService(new DesktopUsageProvider()));
                    singleInstance.Listen(application.Dispatcher, window.ShowFromTray);
                    using (TrayController trayController = new TrayController(window)) { application.Run(window); }
                }
            }
            catch (Exception exception) { AppLogger.Error("Fatal startup failure", exception); }
        }
    }
}
