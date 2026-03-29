using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace FileWatcherAlerts
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            var serviceProcessInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            var serviceInstaller = new ServiceInstaller
            {
                ServiceName = "FileWatcherAlerts",
                DisplayName = "File Watcher Alerts",
                Description = "Monitors Windows shared and NAS directories for file availability and sends email alerts when files are stable.",
                StartType = ServiceStartMode.Automatic
            };

            Installers.Add(serviceProcessInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
