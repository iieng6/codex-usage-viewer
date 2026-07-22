using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CodexUsageViewer.Usage
{
    internal static class UsageCache
    {
        private const int Version = 1;
        private static readonly string FilePath = Path.Combine(AppLogger.DirectoryPath, "usage-cache.json");

        public static CachedUsage Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                using (FileStream stream = File.OpenRead(FilePath))
                {
                    CacheDocument document = (CacheDocument)new DataContractJsonSerializer(typeof(CacheDocument)).ReadObject(stream);
                    if (document == null || document.Version != Version || document.SuccessUnixSeconds <= 0) return null;
                    if (!Valid(document.ShortRemaining) || !Valid(document.LongRemaining)) return null;
                    return new CachedUsage(document.ShortRemaining, document.ShortResetUnixSeconds, document.LongRemaining, document.LongResetUnixSeconds, DateTimeOffset.FromUnixTimeSeconds(document.SuccessUnixSeconds));
                }
            }
            catch (Exception exception) { AppLogger.Error("Cache read failed", exception); return null; }
        }

        public static void Save(CachedUsage value)
        {
            try
            {
                Directory.CreateDirectory(AppLogger.DirectoryPath);
                string temporary = FilePath + ".tmp";
                CacheDocument document = new CacheDocument { Version = Version, ShortRemaining = value.ShortRemaining, ShortResetUnixSeconds = value.ShortResetUnixSeconds, LongRemaining = value.LongRemaining, LongResetUnixSeconds = value.LongResetUnixSeconds, SuccessUnixSeconds = value.SuccessAt.ToUnixTimeSeconds() };
                using (FileStream stream = File.Create(temporary)) new DataContractJsonSerializer(typeof(CacheDocument)).WriteObject(stream, document);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temporary, FilePath);
            }
            catch (Exception exception) { AppLogger.Error("Cache write failed", exception); }
        }

        private static bool Valid(int? value) { return !value.HasValue || (value.Value >= 0 && value.Value <= 100); }

        [DataContract]
        private sealed class CacheDocument
        {
            [DataMember(Name="version")] public int Version { get; set; }
            [DataMember(Name="fiveHourRemaining")] public int? ShortRemaining { get; set; }
            [DataMember(Name="fiveHourResetAt")] public long? ShortResetUnixSeconds { get; set; }
            [DataMember(Name="weekRemaining")] public int? LongRemaining { get; set; }
            [DataMember(Name="weekResetAt")] public long? LongResetUnixSeconds { get; set; }
            [DataMember(Name="lastSuccessAt")] public long SuccessUnixSeconds { get; set; }
        }
    }

    internal sealed class CachedUsage
    {
        public CachedUsage(int? shortRemaining, long? shortReset, int? longRemaining, long? longReset, DateTimeOffset successAt)
        { ShortRemaining = shortRemaining; ShortResetUnixSeconds = shortReset; LongRemaining = longRemaining; LongResetUnixSeconds = longReset; SuccessAt = successAt; }
        public int? ShortRemaining { get; private set; }
        public long? ShortResetUnixSeconds { get; private set; }
        public int? LongRemaining { get; private set; }
        public long? LongResetUnixSeconds { get; private set; }
        public DateTimeOffset SuccessAt { get; private set; }
    }
}
