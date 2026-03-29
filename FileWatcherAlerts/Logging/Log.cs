using System;
using System.Diagnostics;
using System.IO;

namespace FileWatcherAlerts.Logging
{
    public static class Log
    {
        private static readonly object _lock = new object();

        public static void Init(string logFilePath)
        {
            var listener = new TextWriterTraceListener(logFilePath)
            {
                TraceOutputOptions = TraceOptions.None
            };
            Trace.Listeners.Add(listener);
            Trace.Listeners.Add(new ConsoleTraceListener());
            Trace.AutoFlush = true;
        }

        public static void Info(string format, params object[] args)
        {
            Write("INFO", string.Format(format, args));
        }

        public static void Warn(string format, params object[] args)
        {
            Write("WARN", string.Format(format, args));
        }

        public static void Error(string format, params object[] args)
        {
            Write("ERROR", string.Format(format, args));
        }

        private static void Write(string level, string message)
        {
            string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}",
                DateTime.Now, level, message);

            lock (_lock)
            {
                Trace.WriteLine(line);
            }
        }
    }
}
