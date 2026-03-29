using System.Configuration;

namespace FileWatcherAlerts.Configuration
{
    public class WatcherConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("smtp", IsRequired = true)]
        public SmtpElement Smtp
        {
            get { return (SmtpElement)this["smtp"]; }
        }

        [ConfigurationProperty("directories", IsRequired = true)]
        public WatchedDirectoryCollection Directories
        {
            get { return (WatchedDirectoryCollection)this["directories"]; }
        }
    }

    public class SmtpElement : ConfigurationElement
    {
        [ConfigurationProperty("host", IsRequired = true)]
        public string Host
        {
            get { return (string)this["host"]; }
        }

        [ConfigurationProperty("port", DefaultValue = 587)]
        public int Port
        {
            get { return (int)this["port"]; }
        }

        [ConfigurationProperty("useSsl", DefaultValue = true)]
        public bool UseSsl
        {
            get { return (bool)this["useSsl"]; }
        }

        [ConfigurationProperty("username", DefaultValue = "")]
        public string Username
        {
            get { return (string)this["username"]; }
        }

        [ConfigurationProperty("password", DefaultValue = "")]
        public string Password
        {
            get { return (string)this["password"]; }
        }

        [ConfigurationProperty("fromAddress", IsRequired = true)]
        public string FromAddress
        {
            get { return (string)this["fromAddress"]; }
        }
    }
}
