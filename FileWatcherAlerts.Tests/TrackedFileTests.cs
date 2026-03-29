using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FileWatcherAlerts.Monitoring;

namespace FileWatcherAlerts.Tests
{
    [TestClass]
    public class TrackedFileTests
    {
        [TestMethod]
        public void TrackedFile_DefaultAlertSent_IsFalse()
        {
            var file = new TrackedFile();

            Assert.IsFalse(file.AlertSent);
        }

        [TestMethod]
        public void TrackedFile_PropertiesCanBeSetAndRead()
        {
            var writeTime = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
            var stableTime = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

            var file = new TrackedFile
            {
                FullPath = @"\\server\share\data.csv",
                LastKnownSize = 4096,
                LastKnownWriteTimeUtc = writeTime,
                FirstSeenStableUtc = stableTime,
                AlertSent = true,
                DirectoryPath = @"\\server\share"
            };

            Assert.AreEqual(@"\\server\share\data.csv", file.FullPath);
            Assert.AreEqual(4096, file.LastKnownSize);
            Assert.AreEqual(writeTime, file.LastKnownWriteTimeUtc);
            Assert.AreEqual(stableTime, file.FirstSeenStableUtc);
            Assert.IsTrue(file.AlertSent);
            Assert.AreEqual(@"\\server\share", file.DirectoryPath);
        }

        [TestMethod]
        public void TrackedFile_ZeroSizeFile_IsValid()
        {
            var file = new TrackedFile
            {
                FullPath = @"C:\TestDir\empty.txt",
                LastKnownSize = 0,
                LastKnownWriteTimeUtc = DateTime.UtcNow,
                FirstSeenStableUtc = DateTime.UtcNow,
                DirectoryPath = @"C:\TestDir"
            };

            Assert.AreEqual(0, file.LastKnownSize);
        }

        [TestMethod]
        public void TrackedFile_LargeFileSize_IsSupported()
        {
            var file = new TrackedFile
            {
                FullPath = @"C:\TestDir\large.dat",
                LastKnownSize = 10L * 1024 * 1024 * 1024, // 10 GB
                LastKnownWriteTimeUtc = DateTime.UtcNow,
                FirstSeenStableUtc = DateTime.UtcNow,
                DirectoryPath = @"C:\TestDir"
            };

            Assert.AreEqual(10L * 1024 * 1024 * 1024, file.LastKnownSize);
        }
    }
}
