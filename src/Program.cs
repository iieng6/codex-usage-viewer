using System;
using System.Windows;
using CodexUsageViewer.Usage;

namespace CodexUsageViewer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                ProgramNetworkAudit.Write();
            }
            catch
            {
                MessageBox.Show(
                    "无法生成 Program Network Audit。程序将停止运行。",
                    ProgramInfo.Name,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Application application = new Application
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };
            UsageService usageService = new UsageService(new DesktopUsageProvider());
            MainWindow window = new MainWindow(usageService);
            using (TrayController trayController = new TrayController(window))
            {
                application.Run(window);
            }
        }
    }
}
