using System.Configuration;

namespace FileWatcherAlerts.Configuration
{
    [ConfigurationCollection(typeof(WatchedDirectoryElement), AddItemName = "add")]
    public class WatchedDirectoryCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new WatchedDirectoryElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((WatchedDirectoryElement)element).Path;
        }

        public WatchedDirectoryElement this[int index]
        {
            get { return (WatchedDirectoryElement)BaseGet(index); }
        }
    }
}
