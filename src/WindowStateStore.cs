using System;
using System.Globalization;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;

namespace CodexUsageViewer
{
    internal enum DockEdge { Left, Right }

    internal sealed class AppWindowState
    {
        public double Left = double.NaN;
        public double Top = double.NaN;
        public bool Collapsed;
        public DockEdge Edge = DockEdge.Right;
        public bool HalfHidden;
        public double CollapsedOpacity = AppSettings.CollapsedOpacity;
        public bool AutoHideFullScreen = true;
        public bool Topmost = true;
        public string Language = Localization.SystemLanguage;
        public bool HintShown;
    }

    internal static class WindowStateStore
    {
        private static readonly string DirectoryPath = ResolveDirectoryPath();
        private static readonly string FilePath = Path.Combine(DirectoryPath, "window-state.txt");
        private static readonly string FallbackFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "window-state.runtime.txt");

        private static string ResolveDirectoryPath()
        {
            string overridePath = Environment.GetEnvironmentVariable("CODEX_USAGE_VIEWER_DATA_DIR");
            return string.IsNullOrWhiteSpace(overridePath) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexUsageViewer") : overridePath;
        }

        public static AppWindowState Load()
        {
            AppWindowState state = new AppWindowState();
            try
            {
                string path = FilePath;
                if (File.Exists(FallbackFilePath) && (!File.Exists(FilePath) || File.GetLastWriteTimeUtc(FallbackFilePath) > File.GetLastWriteTimeUtc(FilePath))) path = FallbackFilePath;
                if (!File.Exists(path)) return state;
                string[] values = File.ReadAllLines(path);
                if (values.Length >= 2) { TryRead(values[0], out state.Left); TryRead(values[1], out state.Top); }
                for (int i = 2; i < values.Length; i++)
                {
                    int split = values[i].IndexOf('='); if (split < 1) continue;
                    string key = values[i].Substring(0, split); string value = values[i].Substring(split + 1);
                    bool flag; double number;
                    if (key == "collapsed" && bool.TryParse(value, out flag)) state.Collapsed = flag;
                    else if (key == "edge") state.Edge = value == "Left" ? DockEdge.Left : DockEdge.Right;
                    else if (key == "halfHidden" && bool.TryParse(value, out flag)) state.HalfHidden = flag;
                    else if (key == "opacity" && TryRead(value, out number)) state.CollapsedOpacity = Math.Max(.2, Math.Min(1, number));
                    else if (key == "autoHideFullScreen" && bool.TryParse(value, out flag)) state.AutoHideFullScreen = flag;
                    else if (key == "topmost" && bool.TryParse(value, out flag)) state.Topmost = flag;
                    else if (key == "language") state.Language = value;
                    else if (key == "hintShown" && bool.TryParse(value, out flag)) state.HintShown = flag;
                }
            }
            catch { }
            return state;
        }

        public static void Save(AppWindowState state)
        {
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                File.WriteAllLines(FilePath, new[] {
                    state.Left.ToString("R", CultureInfo.InvariantCulture), state.Top.ToString("R", CultureInfo.InvariantCulture),
                    "collapsed=" + state.Collapsed, "edge=" + state.Edge, "halfHidden=" + state.HalfHidden,
                    "opacity=" + state.CollapsedOpacity.ToString("R", CultureInfo.InvariantCulture),
                    "autoHideFullScreen=" + state.AutoHideFullScreen, "topmost=" + state.Topmost, "language=" + state.Language, "hintShown=" + state.HintShown
                });
            }
            catch
            {
                try { WriteState(FallbackFilePath, state); } catch { }
            }
        }

        private static void WriteState(string path, AppWindowState state)
        {
            File.WriteAllLines(path, new[] {
                state.Left.ToString("R", CultureInfo.InvariantCulture), state.Top.ToString("R", CultureInfo.InvariantCulture),
                "collapsed=" + state.Collapsed, "edge=" + state.Edge, "halfHidden=" + state.HalfHidden,
                "opacity=" + state.CollapsedOpacity.ToString("R", CultureInfo.InvariantCulture),
                "autoHideFullScreen=" + state.AutoHideFullScreen, "topmost=" + state.Topmost, "language=" + state.Language, "hintShown=" + state.HintShown
            });
        }

        private static bool TryRead(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && !double.IsNaN(result) && !double.IsInfinity(result);
        }

        public static Rect EnsureVisible(Rect candidate)
        {
            Rect area = GetBestWorkingArea(candidate); double width = Math.Min(candidate.Width, area.Width); double height = Math.Min(candidate.Height, area.Height);
            return new Rect(Math.Max(area.Left, Math.Min(candidate.Left, area.Right - width)), Math.Max(area.Top, Math.Min(candidate.Top, area.Bottom - height)), width, height);
        }

        public static Rect GetBestWorkingArea(Rect candidate)
        {
            double scale = Forms.Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth;
            if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale)) scale = 1;
            Forms.Screen best = Forms.Screen.PrimaryScreen; double bestArea = -1;
            foreach (Forms.Screen screen in Forms.Screen.AllScreens)
            {
                Rect working = new Rect(screen.WorkingArea.Left / scale, screen.WorkingArea.Top / scale, screen.WorkingArea.Width / scale, screen.WorkingArea.Height / scale);
                Rect overlap = Rect.Intersect(candidate, working); double area = overlap.IsEmpty ? 0 : overlap.Width * overlap.Height;
                if (area > bestArea) { bestArea = area; best = screen; }
            }
            return new Rect(best.WorkingArea.Left / scale, best.WorkingArea.Top / scale, best.WorkingArea.Width / scale, best.WorkingArea.Height / scale);
        }
    }
}
