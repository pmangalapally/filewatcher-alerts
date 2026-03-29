using System;
using System.Collections.Generic;
using System.Linq;

namespace FileWatcherAlerts.Monitoring
{
    public class StabilityTracker
    {
        private readonly Dictionary<string, TrackedFile> _trackedFiles =
            new Dictionary<string, TrackedFile>(StringComparer.OrdinalIgnoreCase);

        public TrackedFile UpdateFile(string fullPath, long size, DateTime lastWriteUtc, string directoryPath)
        {
            TrackedFile tracked;
            if (_trackedFiles.TryGetValue(fullPath, out tracked))
            {
                if (tracked.LastKnownSize != size || tracked.LastKnownWriteTimeUtc != lastWriteUtc)
                {
                    // File changed — reset stability clock
                    tracked.LastKnownSize = size;
                    tracked.LastKnownWriteTimeUtc = lastWriteUtc;
                    tracked.FirstSeenStableUtc = DateTime.UtcNow;
                    tracked.AlertSent = false;
                }
            }
            else
            {
                tracked = new TrackedFile
                {
                    FullPath = fullPath,
                    LastKnownSize = size,
                    LastKnownWriteTimeUtc = lastWriteUtc,
                    FirstSeenStableUtc = DateTime.UtcNow,
                    AlertSent = false,
                    DirectoryPath = directoryPath
                };
                _trackedFiles[fullPath] = tracked;
            }

            return tracked;
        }

        public List<TrackedFile> GetStableFiles(string directoryPath, int stabilityMinutes)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-stabilityMinutes);
            return _trackedFiles.Values
                .Where(f => f.DirectoryPath.Equals(directoryPath, StringComparison.OrdinalIgnoreCase)
                            && !f.AlertSent
                            && f.FirstSeenStableUtc <= threshold)
                .ToList();
        }

        public void MarkAlerted(string fullPath)
        {
            TrackedFile tracked;
            if (_trackedFiles.TryGetValue(fullPath, out tracked))
            {
                tracked.AlertSent = true;
            }
        }

        public void PurgeAbsent(string directoryPath, ISet<string> currentFiles)
        {
            var toRemove = _trackedFiles.Keys
                .Where(k => k.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase)
                            && !currentFiles.Contains(k))
                .ToList();

            foreach (var key in toRemove)
            {
                _trackedFiles.Remove(key);
            }
        }
    }
}
