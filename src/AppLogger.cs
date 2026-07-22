using System;
using System.IO;
using System.Text;

namespace CodexUsageViewer
{
    internal static class AppLogger
    {
        private const long MaxBytes = 512 * 1024;
        private static readonly object Gate = new object();
        internal static readonly string DirectoryPath = ResolveDirectoryPath();
        internal static readonly string LogPath = Path.Combine(DirectoryPath, "CodexUsageViewer.log");

        private static string ResolveDirectoryPath()
        {
            string overridePath = Environment.GetEnvironmentVariable("CODEX_USAGE_VIEWER_DATA_DIR");
            return string.IsNullOrWhiteSpace(overridePath) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexUsageViewer") : overridePath;
        }

        public static void Info(string message) { Write("INFO", message, null); }
        public static void Error(string message, Exception exception) { Write("ERROR", message, exception); }

        private static void Write(string level, string message, Exception exception)
        {
            string line = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " [" + level + "] " + message + (exception == null ? string.Empty : " | " + exception.GetType().Name) + Environment.NewLine;
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(DirectoryPath);
                    if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxBytes)
                    {
                        string previous = LogPath + ".1";
                        if (File.Exists(previous)) File.Delete(previous);
                        File.Move(LogPath, previous);
                    }
                    File.AppendAllText(LogPath, line, new UTF8Encoding(false));
                }
            }
            catch
            {
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CodexUsageViewer.runtime.log"), line, new UTF8Encoding(false)); } catch { }
            }
        }
    }
}
