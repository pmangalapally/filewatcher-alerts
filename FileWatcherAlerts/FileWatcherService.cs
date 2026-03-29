using System;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;
using FileWatcherAlerts.Alerting;
using FileWatcherAlerts.Configuration;
using FileWatcherAlerts.Logging;
using FileWatcherAlerts.Monitoring;

namespace FileWatcherAlerts
{
    public class FileWatcherService : ServiceBase
    {
        private Thread _workerThread;
        private volatile bool _running;

        public FileWatcherService()
        {
            ServiceName = "FileWatcherAlerts";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            _running = true;
            _workerThread = new Thread(Run)
            {
                IsBackground = true,
                Name = "FileWatcherPolling"
            };
            _workerThread.Start();
        }

        protected override void OnStop()
        {
            Log.Info("Service stop requested.");
            _running = false;

            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(TimeSpan.FromSeconds(30));
            }

            Log.Info("FileWatcherAlerts service stopped.");
        }

        public void RunAsConsole()
        {
            _running = true;

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _running = false;
                Log.Info("Shutdown signal received. Stopping...");
            };

            Log.Info("FileWatcherAlerts is running in console mode. Press Ctrl+C to stop.");
            Run();
        }

        private void Run()
        {
            var config = (WatcherConfigSection)ConfigurationManager.GetSection("fileWatcher");
            if (config == null)
            {
                Log.Error("Missing <fileWatcher> configuration section in App.config.");
                return;
            }

            if (config.Directories.Count == 0)
            {
                Log.Error("No directories configured to watch.");
                return;
            }

            int pollingIntervalSeconds;
            if (!int.TryParse(ConfigurationManager.AppSettings["PollingIntervalSeconds"], out pollingIntervalSeconds)
                || pollingIntervalSeconds < 1)
            {
                pollingIntervalSeconds = 60;
            }

            Log.Info("FileWatcherAlerts starting up.");
            Log.Info("Polling interval: {0} seconds", pollingIntervalSeconds);
            Log.Info("Monitoring {0} directory(ies):", config.Directories.Count);

            for (int i = 0; i < config.Directories.Count; i++)
            {
                var dir = config.Directories[i];
                Log.Info("  [{0}] Path='{1}', Pattern='{2}', StabilityMinutes={3}, Recipients='{4}'",
                    i + 1, dir.Path, dir.FilePattern, dir.StabilityMinutes, dir.Recipients);
            }

            var tracker = new StabilityTracker();
            var alertSender = new EmailAlertSender(config.Smtp);
            var poller = new FilePoller(tracker, alertSender, config.Directories);

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

                for (int i = 0; i < pollingIntervalSeconds && _running; i++)
                {
                    Thread.Sleep(1000);
                }
            }

            Log.Info("FileWatcherAlerts stopped.");
        }
    }
}
