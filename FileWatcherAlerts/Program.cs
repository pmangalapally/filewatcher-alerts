using System;
using System.Configuration;
using System.Threading;
using FileWatcherAlerts.Alerting;
using FileWatcherAlerts.Configuration;
using FileWatcherAlerts.Logging;
using FileWatcherAlerts.Monitoring;

namespace FileWatcherAlerts
{
    class Program
    {
        private static volatile bool _running = true;

        static void Main(string[] args)
        {
            // Load configuration
            var config = (WatcherConfigSection)ConfigurationManager.GetSection("fileWatcher");
            if (config == null)
            {
                Console.Error.WriteLine("ERROR: Missing <fileWatcher> configuration section in App.config.");
                Environment.Exit(1);
            }

            if (config.Directories.Count == 0)
            {
                Console.Error.WriteLine("ERROR: No directories configured to watch.");
                Environment.Exit(1);
            }

            int pollingIntervalSeconds;
            if (!int.TryParse(ConfigurationManager.AppSettings["PollingIntervalSeconds"], out pollingIntervalSeconds)
                || pollingIntervalSeconds < 1)
            {
                pollingIntervalSeconds = 60;
            }

            string logFilePath = ConfigurationManager.AppSettings["LogFilePath"] ?? "filewatcher.log";

            // Initialize logging
            Log.Init(logFilePath);
            Log.Info("FileWatcherAlerts starting up.");
            Log.Info("Polling interval: {0} seconds", pollingIntervalSeconds);
            Log.Info("Monitoring {0} directory(ies):", config.Directories.Count);

            for (int i = 0; i < config.Directories.Count; i++)
            {
                var dir = config.Directories[i];
                Log.Info("  [{0}] Path='{1}', Pattern='{2}', StabilityMinutes={3}, Recipients='{4}'",
                    i + 1, dir.Path, dir.FilePattern, dir.StabilityMinutes, dir.Recipients);
            }

            // Set up components
            var tracker = new StabilityTracker();
            var alertSender = new EmailAlertSender(config.Smtp);
            var poller = new FilePoller(tracker, alertSender, config.Directories);

            // Graceful shutdown on Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _running = false;
                Log.Info("Shutdown signal received. Stopping...");
            };

            Log.Info("FileWatcherAlerts is running. Press Ctrl+C to stop.");

            // Main polling loop
            while (_running)
            {
                try
                {
                    poller.Poll();
                }
                catch (Exception ex)
                {
                    Log.Error("Unexpected error during polling: {0}", ex);
                }

                // Sleep in small increments so we can respond to shutdown quickly
                for (int i = 0; i < pollingIntervalSeconds && _running; i++)
                {
                    Thread.Sleep(1000);
                }
            }

            Log.Info("FileWatcherAlerts stopped.");
        }
    }
}
