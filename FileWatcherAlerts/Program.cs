using System;
using System.Configuration;
using System.ServiceProcess;
using FileWatcherAlerts.Logging;

namespace FileWatcherAlerts
{
    class Program
    {
        static void Main(string[] args)
        {
            string logFilePath = ConfigurationManager.AppSettings["LogFilePath"] ?? "filewatcher.log";
            Log.Init(logFilePath);

            var service = new FileWatcherService();

            if (Environment.UserInteractive)
            {
                // Running from console (debug mode or direct execution)
                service.RunAsConsole();
            }
            else
            {
                // Running as a Windows Service
                ServiceBase.Run(service);
            }
        }
    }
}
