using System.Configuration;

namespace FileWatcherAlerts.Configuration
{
    public class WatchedDirectoryElement : ConfigurationElement
    {
        [ConfigurationProperty("path", IsRequired = true, IsKey = true)]
        public string Path
        {
            get { return (string)this["path"]; }
        }

        [ConfigurationProperty("filePattern", DefaultValue = "*")]
        public string FilePattern
        {
            get { return (string)this["filePattern"]; }
        }

        [ConfigurationProperty("stabilityMinutes", DefaultValue = 30, IsRequired = true)]
        [IntegerValidator(MinValue = 1)]
        public int StabilityMinutes
        {
            get { return (int)this["stabilityMinutes"]; }
        }

        [ConfigurationProperty("recipients", IsRequired = true)]
        public string Recipients
        {
            get { return (string)this["recipients"]; }
        }
    }
}
