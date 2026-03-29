using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FileWatcherAlerts.Monitoring;

namespace FileWatcherAlerts.Tests
{
    [TestClass]
    public class StabilityTrackerTests
    {
        private StabilityTracker _tracker;
        private const string TestDir = @"C:\TestDir";

        [TestInitialize]
        public void SetUp()
        {
            _tracker = new StabilityTracker();
        }

        [TestMethod]
        public void UpdateFile_NewFile_CreatesTrackedEntry()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var result = _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime, TestDir);

            Assert.IsNotNull(result);
            Assert.AreEqual(@"C:\TestDir\file.csv", result.FullPath);
            Assert.AreEqual(1024, result.LastKnownSize);
            Assert.AreEqual(writeTime, result.LastKnownWriteTimeUtc);
            Assert.IsFalse(result.AlertSent);
            Assert.AreEqual(TestDir, result.DirectoryPath);
        }

        [TestMethod]
        public void UpdateFile_SameFileUnchanged_DoesNotResetStabilityClock()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var first = _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime, TestDir);
            var originalStableTime = first.FirstSeenStableUtc;

            Thread.Sleep(50);

            var second = _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime, TestDir);

            Assert.AreEqual(originalStableTime, second.FirstSeenStableUtc);
            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void UpdateFile_SizeChanged_ResetsStabilityClock()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var first = _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime, TestDir);
            var originalStableTime = first.FirstSeenStableUtc;

            Thread.Sleep(50);

            var second = _tracker.UpdateFile(@"C:\TestDir\file.csv", 2048, writeTime, TestDir);

            Assert.IsTrue(second.FirstSeenStableUtc > originalStableTime);
            Assert.IsFalse(second.AlertSent);
        }

        [TestMethod]
        public void UpdateFile_WriteTimeChanged_ResetsStabilityClock()
        {
            var writeTime1 = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var writeTime2 = new DateTime(2025, 1, 1, 12, 5, 0, DateTimeKind.Utc);

            var first = _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime1, TestDir);
            var originalStableTime = first.FirstSeenStableUtc;

            Thread.Sleep(50);

            var second = _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime2, TestDir);

            Assert.IsTrue(second.FirstSeenStableUtc > originalStableTime);
        }

        [TestMethod]
        public void UpdateFile_AfterAlertSent_FileChanges_ResetsAlertSent()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime, TestDir);
            _tracker.MarkAlerted(@"C:\TestDir\file.csv");

            var updated = _tracker.UpdateFile(@"C:\TestDir\file.csv", 2048, writeTime, TestDir);

            Assert.IsFalse(updated.AlertSent);
        }

        [TestMethod]
        public void GetStableFiles_NoFilesStableYet_ReturnsEmpty()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime, TestDir);

            var stable = _tracker.GetStableFiles(TestDir, 60);

            Assert.AreEqual(0, stable.Count);
        }

        [TestMethod]
        public void GetStableFiles_FileStableLongEnough_ReturnsFile()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var tracked = _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime, TestDir);

            // Simulate file being stable for longer than threshold by backdating FirstSeenStableUtc
            tracked.FirstSeenStableUtc = DateTime.UtcNow.AddMinutes(-120);

            var stable = _tracker.GetStableFiles(TestDir, 60);

            Assert.AreEqual(1, stable.Count);
            Assert.AreEqual(@"C:\TestDir\file.csv", stable[0].FullPath);
        }

        [TestMethod]
        public void GetStableFiles_AlreadyAlerted_ExcludesFile()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var tracked = _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime, TestDir);
            tracked.FirstSeenStableUtc = DateTime.UtcNow.AddMinutes(-120);
            _tracker.MarkAlerted(@"C:\TestDir\file.csv");

            var stable = _tracker.GetStableFiles(TestDir, 60);

            Assert.AreEqual(0, stable.Count);
        }

        [TestMethod]
        public void GetStableFiles_DifferentDirectory_ExcludesFile()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var tracked = _tracker.UpdateFile(@"C:\OtherDir\file.csv", 1024, writeTime, @"C:\OtherDir");
            tracked.FirstSeenStableUtc = DateTime.UtcNow.AddMinutes(-120);

            var stable = _tracker.GetStableFiles(TestDir, 60);

            Assert.AreEqual(0, stable.Count);
        }

        [TestMethod]
        public void MarkAlerted_SetsAlertSentTrue()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var tracked = _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime, TestDir);

            _tracker.MarkAlerted(@"C:\TestDir\file.csv");

            Assert.IsTrue(tracked.AlertSent);
        }

        [TestMethod]
        public void MarkAlerted_NonExistentFile_DoesNotThrow()
        {
            _tracker.MarkAlerted(@"C:\TestDir\nonexistent.csv");
            // Should complete without exception
        }

        [TestMethod]
        public void PurgeAbsent_RemovesFilesNotInCurrentSet()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _tracker.UpdateFile(@"C:\TestDir\keep.csv", 1024, writeTime, TestDir);
            _tracker.UpdateFile(@"C:\TestDir\remove.csv", 512, writeTime, TestDir);

            var currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                @"C:\TestDir\keep.csv"
            };

            _tracker.PurgeAbsent(TestDir, currentFiles);

            // "keep.csv" should still return a tracked entry when updated
            var kept = _tracker.UpdateFile(@"C:\TestDir\keep.csv", 1024, writeTime, TestDir);
            Assert.IsNotNull(kept);

            // "remove.csv" should be treated as a new file (fresh stability clock)
            var removed = _tracker.UpdateFile(@"C:\TestDir\remove.csv", 512, writeTime, TestDir);
            Assert.IsFalse(removed.AlertSent);
            // FirstSeenStableUtc should be very recent (just re-created)
            Assert.IsTrue((DateTime.UtcNow - removed.FirstSeenStableUtc).TotalSeconds < 5);
        }

        [TestMethod]
        public void PurgeAbsent_DoesNotAffectOtherDirectories()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _tracker.UpdateFile(@"C:\TestDir\file1.csv", 1024, writeTime, TestDir);
            var otherTracked = _tracker.UpdateFile(@"C:\OtherDir\file2.csv", 512, writeTime, @"C:\OtherDir");
            var originalStableTime = otherTracked.FirstSeenStableUtc;

            // Purge TestDir with empty set — should remove file1 but not file2
            _tracker.PurgeAbsent(TestDir, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            // file2 should still be tracked with original stability time
            var stillTracked = _tracker.UpdateFile(@"C:\OtherDir\file2.csv", 512, writeTime, @"C:\OtherDir");
            Assert.AreEqual(originalStableTime, stillTracked.FirstSeenStableUtc);
        }

        [TestMethod]
        public void UpdateFile_CaseInsensitivePaths()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var first = _tracker.UpdateFile(@"C:\TestDir\FILE.csv", 1024, writeTime, TestDir);
            var second = _tracker.UpdateFile(@"C:\TestDir\file.csv", 1024, writeTime, TestDir);

            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void GetStableFiles_MultipleStableFiles_ReturnsAll()
        {
            var writeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var t1 = _tracker.UpdateFile(@"C:\TestDir\file1.csv", 100, writeTime, TestDir);
            var t2 = _tracker.UpdateFile(@"C:\TestDir\file2.csv", 200, writeTime, TestDir);
            var t3 = _tracker.UpdateFile(@"C:\TestDir\file3.csv", 300, writeTime, TestDir);

            t1.FirstSeenStableUtc = DateTime.UtcNow.AddMinutes(-120);
            t2.FirstSeenStableUtc = DateTime.UtcNow.AddMinutes(-120);
            t3.FirstSeenStableUtc = DateTime.UtcNow.AddMinutes(-120);

            _tracker.MarkAlerted(@"C:\TestDir\file2.csv");

            var stable = _tracker.GetStableFiles(TestDir, 60);

            Assert.AreEqual(2, stable.Count);
        }
    }
}
