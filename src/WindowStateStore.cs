using System;
using System.Globalization;
using System.IO;
using System.Windows;

namespace CodexUsageViewer
{
    internal static class WindowStateStore
    {
        private static readonly string DirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodexUsageViewer");
        private static readonly string FilePath = Path.Combine(DirectoryPath, "window-state.txt");

        public static void Restore(Window window)
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return;
                }

                string[] values = File.ReadAllLines(FilePath);
                if (values.Length != 4)
                {
                    return;
                }

                double left;
                double top;
                double width;
                double height;
                if (!TryRead(values[0], out left) || !TryRead(values[1], out top) ||
                    !TryRead(values[2], out width) || !TryRead(values[3], out height))
                {
                    return;
                }

                Rect candidate = new Rect(left, top, width, height);
                if (width >= window.MinWidth && height >= window.MinHeight && IntersectsWorkingArea(candidate))
                {
                    window.Left = left;
                    window.Top = top;
                    window.Width = width;
                    window.Height = height;
                    window.WindowStartupLocation = WindowStartupLocation.Manual;
                }
            }
            catch
            {
            }
        }

        public static void Save(Window window)
        {
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                Rect bounds = window.RestoreBounds;
                string[] values =
                {
                    bounds.Left.ToString("R", CultureInfo.InvariantCulture),
                    bounds.Top.ToString("R", CultureInfo.InvariantCulture),
                    bounds.Width.ToString("R", CultureInfo.InvariantCulture),
                    bounds.Height.ToString("R", CultureInfo.InvariantCulture)
                };
                File.WriteAllLines(FilePath, values);
            }
            catch
            {
            }
        }

        private static bool TryRead(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) &&
                   !double.IsNaN(result) && !double.IsInfinity(result);
        }

        private static bool IntersectsWorkingArea(Rect candidate)
        {
            return candidate.IntersectsWith(SystemParameters.WorkArea);
        }
    }
}
