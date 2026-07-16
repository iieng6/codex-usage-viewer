using System;
using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace CodexUsageViewer
{
    internal sealed class TrayController : IDisposable
    {
        private readonly MainWindow window;
        private readonly Forms.NotifyIcon notifyIcon;
        private bool disposed;

        public TrayController(MainWindow window)
        {
            this.window = window;
            Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Show", null, OnShow);
            menu.Items.Add("Refresh", null, OnRefresh);
            menu.Items.Add("About", null, OnAbout);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Exit", null, OnExit);

            notifyIcon = new Forms.NotifyIcon
            {
                Text = ProgramInfo.Name,
                Icon = SystemIcons.Application,
                ContextMenuStrip = menu,
                Visible = true
            };
            notifyIcon.DoubleClick += OnDoubleClick;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        private void OnShow(object sender, EventArgs e)
        {
            window.Dispatcher.BeginInvoke(new Action(window.ShowFromTray));
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            window.Dispatcher.BeginInvoke(new Action(window.RefreshFromTray));
        }

        private void OnAbout(object sender, EventArgs e)
        {
            window.Dispatcher.BeginInvoke(new Action(window.ShowAbout));
        }

        private void OnExit(object sender, EventArgs e)
        {
            window.Dispatcher.BeginInvoke(new Action(ExitApplication));
        }

        private void OnDoubleClick(object sender, EventArgs e)
        {
            window.Dispatcher.BeginInvoke(new Action(window.ShowFromTray));
        }

        private void ExitApplication()
        {
            Dispose();
            window.RequestExit();
            Application.Current.Shutdown();
        }
    }
}
