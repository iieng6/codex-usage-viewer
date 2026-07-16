using System;

namespace CodexUsageViewer.Usage
{
    internal sealed class UsageWindow
    {
        public UsageWindow(int usedPercent, long resetsAt, long durationMinutes)
        {
            UsedPercent = usedPercent;
            ResetsAt = resetsAt;
            DurationMinutes = durationMinutes;
        }

        public int UsedPercent { get; private set; }
        public long ResetsAt { get; private set; }
        public long DurationMinutes { get; private set; }
    }

    internal sealed class UsageSnapshot
    {
        public UsageSnapshot(UsageWindow shortWindow, UsageWindow longWindow)
        {
            ShortWindow = shortWindow;
            LongWindow = longWindow;
        }

        public UsageWindow ShortWindow { get; private set; }
        public UsageWindow LongWindow { get; private set; }
    }
}
