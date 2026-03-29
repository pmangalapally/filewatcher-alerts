using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileWatcherAlerts.Alerting;
using FileWatcherAlerts.Configuration;
using FileWatcherAlerts.Logging;

namespace FileWatcherAlerts.Monitoring
{
    public class FilePoller
    {
        private readonly StabilityTracker _tracker;
        private readonly EmailAlertSender _alertSender;
        private readonly WatchedDirectoryCollection _directories;

        public FilePoller(StabilityTracker tracker, EmailAlertSender alertSender,
            WatchedDirectoryCollection directories)
        {
            _tracker = tracker;
            _alertSender = alertSender;
            _directories = directories;
        }

        public void Poll()
        {
            for (int i = 0; i < _directories.Count; i++)
            {
                var dir = _directories[i];
                PollDirectory(dir);
            }
        }

        private void PollDirectory(WatchedDirectoryElement dir)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(dir.Path, dir.FilePattern);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Log.Warn("Cannot access directory '{0}': {1}", dir.Path, ex.Message);
                return;
            }

            var currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var filePath in files)
            {
                try
                {
                    var info = new FileInfo(filePath);
                    _tracker.UpdateFile(filePath, info.Length, info.LastWriteTimeUtc, dir.Path);
                    currentFiles.Add(filePath);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    Log.Warn("Cannot read file '{0}': {1}", filePath, ex.Message);
                }
            }

            _tracker.PurgeAbsent(dir.Path, currentFiles);

            var stableFiles = _tracker.GetStableFiles(dir.Path, dir.StabilityMinutes);
            if (stableFiles.Any())
            {
                Log.Info("Found {0} stable file(s) in '{1}'", stableFiles.Count, dir.Path);

                bool sent = _alertSender.SendAlert(dir, stableFiles);
                if (sent)
                {
                    foreach (var f in stableFiles)
                    {
                        _tracker.MarkAlerted(f.FullPath);
                    }
                }
            }
        }
    }
}
