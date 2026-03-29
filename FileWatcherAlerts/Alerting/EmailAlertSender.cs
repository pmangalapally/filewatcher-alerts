using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using FileWatcherAlerts.Configuration;
using FileWatcherAlerts.Logging;
using FileWatcherAlerts.Monitoring;

namespace FileWatcherAlerts.Alerting
{
    public class EmailAlertSender
    {
        private readonly string _host;
        private readonly int _port;
        private readonly bool _useSsl;
        private readonly string _username;
        private readonly string _password;
        private readonly string _fromAddress;

        public EmailAlertSender(SmtpElement smtpConfig)
        {
            _host = smtpConfig.Host;
            _port = smtpConfig.Port;
            _useSsl = smtpConfig.UseSsl;
            _username = smtpConfig.Username;
            _password = smtpConfig.Password;
            _fromAddress = smtpConfig.FromAddress;
        }

        public bool SendAlert(WatchedDirectoryElement directory, List<TrackedFile> stableFiles)
        {
            var recipients = directory.Recipients
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => r.Length > 0)
                .ToList();

            if (!recipients.Any())
            {
                Log.Warn("No recipients configured for directory '{0}', skipping alert.", directory.Path);
                return false;
            }

            string subject = string.Format("FileWatcher Alert: {0} stable file(s) in {1}",
                stableFiles.Count, directory.Path);

            var body = new StringBuilder();
            body.AppendLine("The following file(s) have been stable for the configured threshold");
            body.AppendFormat("({0} minutes) in directory: {1}", directory.StabilityMinutes, directory.Path);
            body.AppendLine();
            body.AppendLine();
            body.AppendLine("--------------------------------------------------------------");

            foreach (var file in stableFiles)
            {
                body.AppendFormat("  File:          {0}", file.FullPath);
                body.AppendLine();
                body.AppendFormat("  Size:          {0:N0} bytes", file.LastKnownSize);
                body.AppendLine();
                body.AppendFormat("  Last Modified: {0:yyyy-MM-dd HH:mm:ss} UTC", file.LastKnownWriteTimeUtc);
                body.AppendLine();
                body.AppendFormat("  Stable Since:  {0:yyyy-MM-dd HH:mm:ss} UTC", file.FirstSeenStableUtc);
                body.AppendLine();
                body.AppendLine("--------------------------------------------------------------");
            }

            body.AppendLine();
            body.AppendLine("This is an automated alert from FileWatcherAlerts.");

            try
            {
                using (var client = new SmtpClient(_host, _port))
                {
                    client.EnableSsl = _useSsl;
                    if (!string.IsNullOrEmpty(_username))
                    {
                        client.Credentials = new NetworkCredential(_username, _password);
                    }

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_fromAddress);
                        foreach (var recipient in recipients)
                        {
                            message.To.Add(recipient);
                        }
                        message.Subject = subject;
                        message.Body = body.ToString();
                        message.IsBodyHtml = false;

                        client.Send(message);
                    }
                }

                Log.Info("Alert email sent to [{0}] for {1} file(s) in '{2}'.",
                    string.Join(", ", recipients), stableFiles.Count, directory.Path);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to send alert email for directory '{0}': {1}", directory.Path, ex.Message);
                return false;
            }
        }
    }
}
