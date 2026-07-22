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
        private Forms.ContextMenuStrip menu;
        private bool disposed;

        public TrayController(MainWindow window)
        {
            this.window = window;
            notifyIcon = new Forms.NotifyIcon { Text = ProgramInfo.Name, Icon = SystemIcons.Application, Visible = true };
            notifyIcon.DoubleClick += delegate { window.Dispatcher.BeginInvoke(new Action(window.ShowFromTray)); };
            window.DisplayChanged += OnDisplayChanged; Localization.Changed += OnLanguageChanged;
            RebuildMenu(); UpdateText();
        }

        private void RebuildMenu()
        {
            Forms.ContextMenuStrip replacement = new Forms.ContextMenuStrip();
            Forms.ToolStripMenuItem collapse = (Forms.ToolStripMenuItem)replacement.Items.Add(window.IsCollapsed ? Localization.Get("Expand") : Localization.Get("Collapse"), null, delegate { window.Dispatcher.BeginInvoke(new Action(window.ToggleCollapsed)); });
            replacement.Items.Add(Localization.Get("Show"), null, delegate { window.Dispatcher.BeginInvoke(new Action(window.ShowFromTray)); });
            replacement.Items.Add(Localization.Get("HideToTray"), null, delegate { window.Dispatcher.BeginInvoke(new Action(window.HideTemporarily)); });
            Forms.ToolStripMenuItem topmost = (Forms.ToolStripMenuItem)replacement.Items.Add(Localization.Get("AlwaysOnTop"), null, delegate { window.Dispatcher.BeginInvoke(new Action(window.ToggleTopmost)); });
            Forms.ToolStripMenuItem fullScreen = (Forms.ToolStripMenuItem)replacement.Items.Add(Localization.Get("AutoHideFullscreen"), null, delegate { window.Dispatcher.BeginInvoke(new Action(window.ToggleAutoHideFullScreen)); });
            replacement.Items.Add(Localization.Get("RefreshNow"), null, delegate { window.Dispatcher.BeginInvoke(new Action(async delegate { await window.RefreshFromTray(); })); });
            Forms.ToolStripMenuItem language = new Forms.ToolStripMenuItem(Localization.Get("Language"));
            AddLanguageItem(language, Localization.Get("FollowSystem"), Localization.SystemLanguage);
            AddLanguageItem(language, Localization.Get("SimplifiedChinese"), Localization.ChineseLanguage);
            AddLanguageItem(language, Localization.Get("English"), Localization.EnglishLanguage);
            replacement.Items.Add(language);
            replacement.Items.Add(new Forms.ToolStripSeparator());
            replacement.Items.Add(Localization.Get("Exit"), null, delegate { window.Dispatcher.BeginInvoke(new Action(ExitApplication)); });
            replacement.Opening += delegate { collapse.Text = window.IsCollapsed ? Localization.Get("Expand") : Localization.Get("Collapse"); topmost.Checked = window.IsAlwaysOnTop; fullScreen.Checked = window.IsAutoHideFullScreen; };
            Forms.ContextMenuStrip old = menu; menu = replacement; notifyIcon.ContextMenuStrip = menu; if (old != null) old.Dispose();
            AppLogger.Info("Tray menu rebuilt; language=" + Localization.EffectiveLanguage + "; preference=" + Localization.Preference);
        }

        private void AddLanguageItem(Forms.ToolStripMenuItem parent, string text, string value)
        {
            Forms.ToolStripMenuItem item = new Forms.ToolStripMenuItem(text) { Checked = Localization.Preference == value };
            item.Click += delegate { window.Dispatcher.BeginInvoke(new Action(delegate { window.SetLanguage(value); })); };
            parent.DropDownItems.Add(item);
        }

        public void Dispose() { if (disposed) return; disposed = true; window.DisplayChanged -= OnDisplayChanged; Localization.Changed -= OnLanguageChanged; notifyIcon.Visible = false; notifyIcon.Dispose(); if (menu != null) menu.Dispose(); }
        private void OnDisplayChanged(object sender, EventArgs e) { UpdateText(); }
        private void OnLanguageChanged(object sender, EventArgs e) { RebuildMenu(); UpdateText(); }
        private void UpdateText() { string text = window.TrayText; notifyIcon.Text = text.Length > 63 ? text.Substring(0, 63) : text; }
        private void ExitApplication() { Dispose(); window.RequestExit(); Application.Current.Shutdown(); }
    }
}
