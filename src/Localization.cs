using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace CodexUsageViewer
{
    internal static class Localization
    {
        public const string SystemLanguage = "system";
        public const string ChineseLanguage = "zh-CN";
        public const string EnglishLanguage = "en-US";
        private static readonly Dictionary<string, string> English = Load("CodexUsageViewer.Resources.Strings.en-US.txt");
        private static readonly Dictionary<string, string> Chinese = Load("CodexUsageViewer.Resources.Strings.zh-CN.txt");
        private static string preference = SystemLanguage;
        public static event EventHandler Changed;

        public static string Preference { get { return preference; } }
        public static string EffectiveLanguage { get { return Resolve(preference, CultureInfo.CurrentUICulture); } }

        public static void SetPreference(string value)
        {
            string normalized = value == ChineseLanguage || value == EnglishLanguage ? value : SystemLanguage;
            if (preference == normalized) return;
            preference = normalized;
            EventHandler handler = Changed; if (handler != null) handler(null, EventArgs.Empty);
        }

        public static string Get(string key)
        {
            string value;
            if (EffectiveLanguage == ChineseLanguage && Chinese.TryGetValue(key, out value)) return value;
            return English.TryGetValue(key, out value) ? value : "";
        }

        public static string Format(string key, params object[] values) { return string.Format(CultureInfo.CurrentCulture, Get(key), values); }

        internal static string Resolve(string value, CultureInfo systemCulture)
        {
            if (value == ChineseLanguage || value == EnglishLanguage) return value;
            string name = (systemCulture == null ? "" : systemCulture.Name).ToLowerInvariant();
            return name == "zh-cn" || name == "zh-sg" || name.StartsWith("zh-hans") ? ChineseLanguage : EnglishLanguage;
        }

        private static Dictionary<string, string> Load(string resourceName)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null) return values;
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        int split = line.IndexOf('='); if (split <= 0 || line.StartsWith("#")) continue;
                        values[line.Substring(0, split)] = line.Substring(split + 1);
                    }
                }
            }
            return values;
        }
    }
}
