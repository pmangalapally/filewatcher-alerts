using System;

namespace FileWatcherAlerts.Monitoring
{
    public class TrackedFile
    {
        public string FullPath { get; set; }
        public long LastKnownSize { get; set; }
        public DateTime LastKnownWriteTimeUtc { get; set; }
        public DateTime FirstSeenStableUtc { get; set; }
        public bool AlertSent { get; set; }
        public string DirectoryPath { get; set; }
    }
}
