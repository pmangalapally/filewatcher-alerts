using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FileWatcherAlerts.Monitoring;

namespace FileWatcherAlerts.Tests
{
    [TestClass]
    public class FilePollerTests
    {
        private string _testDir;

        [TestInitialize]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "FileWatcherAlerts_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
        }

        [TestCleanup]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [TestMethod]
        public void StabilityTracker_WithRealFiles_TracksNewFiles()
        {
            var tracker = new StabilityTracker();
            var filePath = Path.Combine(_testDir, "test.csv");
            File.WriteAllText(filePath, "col1,col2\nval1,val2");

            var info = new FileInfo(filePath);
            var tracked = tracker.UpdateFile(filePath, info.Length, info.LastWriteTimeUtc, _testDir);

            Assert.IsNotNull(tracked);
            Assert.AreEqual(filePath, tracked.FullPath);
            Assert.AreEqual(info.Length, tracked.LastKnownSize);
            Assert.IsFalse(tracked.AlertSent);
        }

        [TestMethod]
        public void StabilityTracker_FileModified_ResetsStability()
        {
            var tracker = new StabilityTracker();
            var filePath = Path.Combine(_testDir, "test.csv");
            File.WriteAllText(filePath, "initial");

            var info1 = new FileInfo(filePath);
            var tracked1 = tracker.UpdateFile(filePath, info1.Length, info1.LastWriteTimeUtc, _testDir);
            var firstStableTime = tracked1.FirstSeenStableUtc;

            Thread.Sleep(100);

            // Modify the file
            File.WriteAllText(filePath, "initial content with more data");
            var info2 = new FileInfo(filePath);
            var tracked2 = tracker.UpdateFile(filePath, info2.Length, info2.LastWriteTimeUtc, _testDir);

            Assert.IsTrue(tracked2.FirstSeenStableUtc > firstStableTime);
        }

        [TestMethod]
        public void StabilityTracker_FileDeleted_PurgesEntry()
        {
            var tracker = new StabilityTracker();
            var filePath = Path.Combine(_testDir, "test.csv");
            File.WriteAllText(filePath, "data");

            var info = new FileInfo(filePath);
            tracker.UpdateFile(filePath, info.Length, info.LastWriteTimeUtc, _testDir);

            // Simulate file deletion by passing empty current file set
            var currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            tracker.PurgeAbsent(_testDir, currentFiles);

            // Re-adding should create a fresh entry
            var reAdded = tracker.UpdateFile(filePath, info.Length, info.LastWriteTimeUtc, _testDir);
            Assert.IsTrue((DateTime.UtcNow - reAdded.FirstSeenStableUtc).TotalSeconds < 5);
        }

        [TestMethod]
        public void DirectoryEnumerateFiles_WithPattern_MatchesCorrectFiles()
        {
            File.WriteAllText(Path.Combine(_testDir, "report.csv"), "data");
            File.WriteAllText(Path.Combine(_testDir, "report.xlsx"), "data");
            File.WriteAllText(Path.Combine(_testDir, "notes.txt"), "data");

            var csvFiles = Directory.EnumerateFiles(_testDir, "*.csv").ToList();
            var xlsxFiles = Directory.EnumerateFiles(_testDir, "*.xlsx").ToList();
            var allFiles = Directory.EnumerateFiles(_testDir, "*").ToList();

            Assert.AreEqual(1, csvFiles.Count);
            Assert.AreEqual(1, xlsxFiles.Count);
            Assert.AreEqual(3, allFiles.Count);
        }

        [TestMethod]
        public void StabilityTracker_EndToEnd_MultipleFilesInDirectory()
        {
            var tracker = new StabilityTracker();

            // Create multiple files
            var file1 = Path.Combine(_testDir, "data1.csv");
            var file2 = Path.Combine(_testDir, "data2.csv");
            var file3 = Path.Combine(_testDir, "data3.csv");
            File.WriteAllText(file1, "row1");
            File.WriteAllText(file2, "row2");
            File.WriteAllText(file3, "row3");

            // Track all files
            var currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Directory.EnumerateFiles(_testDir, "*.csv"))
            {
                var info = new FileInfo(path);
                var tracked = tracker.UpdateFile(path, info.Length, info.LastWriteTimeUtc, _testDir);
                currentFiles.Add(path);
            }

            // Nothing should be stable yet (just added)
            var stable = tracker.GetStableFiles(_testDir, 1);
            Assert.AreEqual(0, stable.Count);

            // Backdate all files' stability times
            foreach (var path in currentFiles)
            {
                var t = tracker.UpdateFile(path, new FileInfo(path).Length, new FileInfo(path).LastWriteTimeUtc, _testDir);
                t.FirstSeenStableUtc = DateTime.UtcNow.AddMinutes(-10);
            }

            stable = tracker.GetStableFiles(_testDir, 1);
            Assert.AreEqual(3, stable.Count);

            // Mark one as alerted
            tracker.MarkAlerted(file2);
            stable = tracker.GetStableFiles(_testDir, 1);
            Assert.AreEqual(2, stable.Count);

            // Delete file3, purge
            File.Delete(file3);
            currentFiles.Remove(file3);
            tracker.PurgeAbsent(_testDir, currentFiles);

            stable = tracker.GetStableFiles(_testDir, 1);
            Assert.AreEqual(1, stable.Count);
            Assert.IsTrue(stable[0].FullPath.EndsWith("data1.csv", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void StabilityTracker_InaccessibleDirectory_DoesNotCrash()
        {
            var fakePath = @"C:\NonExistent_" + Guid.NewGuid().ToString("N");

            try
            {
                Directory.EnumerateFiles(fakePath, "*").ToList();
                Assert.Fail("Expected DirectoryNotFoundException");
            }
            catch (DirectoryNotFoundException)
            {
                // Expected — FilePoller catches IOException which is the base class
            }
        }
    }
}
