# FileWatcher Alerts

A .NET Framework 4.8 application that monitors Windows shared directories (UNC paths) and NAS folders for file availability. When files appear and remain unchanged for a configurable stability period, email alerts are sent to notify your team.

Runs as a **console application** for development/debugging, or as a **Windows Service** in production.

## Features

- **Polling-based monitoring** — reliable on SMB/CIFS network shares and NAS devices where `FileSystemWatcher` is not dependable
- **Configurable stability thresholds** — alert only after a file has been unchanged for N minutes (supports minutes, hours, days)
- **Multiple directory support** — monitor several paths with independent file patterns, thresholds, and recipient lists
- **Email alerts via SMTP** — plain-text emails listing file details (path, size, timestamps)
- **Dual-mode execution** — run interactively from a console or install as a Windows Service
- **Zero external dependencies** — built entirely on .NET Framework BCL

## Prerequisites

- Windows OS with .NET Framework 4.8 installed
- Visual Studio 2019+ or MSBuild for building
- Network access to the shared directories being monitored
- SMTP server access for sending alert emails

## Quick Start

### 1. Build

```cmd
msbuild FileWatcherAlerts.sln /p:Configuration=Release
```

Or open `FileWatcherAlerts.sln` in Visual Studio and build.

### 2. Configure

Edit `FileWatcherAlerts.exe.config` (built from `App.config`) in the output directory:

```xml
<configuration>
  <appSettings>
    <!-- Polling frequency in seconds -->
    <add key="PollingIntervalSeconds" value="60" />
    <!-- Log file location -->
    <add key="LogFilePath" value="C:\Logs\filewatcher.log" />
  </appSettings>

  <fileWatcher>
    <!-- SMTP server settings -->
    <smtp host="smtp.company.com"
          port="587"
          useSsl="true"
          username="alerts@company.com"
          password="your-password"
          fromAddress="alerts@company.com" />

    <!-- Directories to monitor -->
    <directories>
      <add path="\\fileserver\incoming\reports"
           filePattern="*.csv"
           stabilityMinutes="30"
           recipients="team@company.com;ops@company.com" />

      <add path="\\nas01\shared\uploads"
           filePattern="*"
           stabilityMinutes="1440"
           recipients="admin@company.com" />
    </directories>
  </fileWatcher>
</configuration>
```

### 3. Run

**Console mode** (for testing/debugging):

```cmd
FileWatcherAlerts.exe
```

Press `Ctrl+C` to stop gracefully.

**Windows Service** (for production):

```cmd
:: Install (run as Administrator)
C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe FileWatcherAlerts.exe

:: Start the service
net start FileWatcherAlerts

:: Stop the service
net stop FileWatcherAlerts

:: Uninstall
C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe /u FileWatcherAlerts.exe
```

After installation, the service can also be managed through `services.msc`.

## Configuration Reference

### App Settings

| Key | Default | Description |
|-----|---------|-------------|
| `PollingIntervalSeconds` | `60` | How often to scan directories (in seconds) |
| `LogFilePath` | `filewatcher.log` | Path to the log file (relative or absolute) |

### SMTP Settings (`<smtp>`)

| Attribute | Required | Default | Description |
|-----------|----------|---------|-------------|
| `host` | Yes | — | SMTP server hostname |
| `port` | No | `587` | SMTP server port |
| `useSsl` | No | `true` | Enable TLS/SSL |
| `username` | No | `""` | SMTP authentication username (omit for anonymous) |
| `password` | No | `""` | SMTP authentication password |
| `fromAddress` | Yes | — | Sender email address |

### Directory Settings (`<directories><add>`)

| Attribute | Required | Default | Description |
|-----------|----------|---------|-------------|
| `path` | Yes | — | UNC path (`\\server\share`) or mapped drive (`Z:\folder`) |
| `filePattern` | No | `*` | Wildcard filter (e.g., `*.csv`, `*.xlsx`, `report_*.*`) |
| `stabilityMinutes` | Yes | `30` | Minutes a file must be unchanged before alerting |
| `recipients` | Yes | — | Semicolon-delimited email addresses |

**Stability threshold examples:**
- 30 minutes: `stabilityMinutes="30"`
- 2 hours: `stabilityMinutes="120"`
- 1 day: `stabilityMinutes="1440"`
- 1 week: `stabilityMinutes="10080"`

## How It Works

```
┌─────────────────────────────────────────────────┐
│                  Program.cs                     │
│  Detects console vs. service mode               │
│  Initializes logging                            │
└──────────────────────┬──────────────────────────┘
                       │
          ┌────────────▼────────────┐
          │   FileWatcherService    │
          │  (ServiceBase)          │
          │  Runs polling loop on   │
          │  background thread      │
          └────────────┬────────────┘
                       │
             ┌─────────▼─────────┐
             │    FilePoller     │      Every N seconds:
             │                   │
             │  For each dir:    │  ──► Enumerate files matching pattern
             │    Update tracker │  ──► Track size + last-write-time
             │    Purge absent   │  ──► Remove vanished files
             │    Check stable   │  ──► Find files stable > threshold
             │    Send alerts    │  ──► Email recipients
             └────────┬──┬───────┘
                      │  │
         ┌────────────┘  └────────────┐
         ▼                            ▼
┌─────────────────┐         ┌─────────────────┐
│StabilityTracker │         │EmailAlertSender  │
│                 │         │                  │
│ In-memory dict  │         │ System.Net.Mail  │
│ of TrackedFile  │         │ SmtpClient       │
│ entries         │         │                  │
└─────────────────┘         └─────────────────┘
```

1. **Enumerate**: `Directory.EnumerateFiles` scans each configured path with the file pattern
2. **Track**: Each file's size and last-write-time are compared to the previous poll. If changed, the stability clock resets
3. **Stabilize**: Once a file remains unchanged for `stabilityMinutes`, it qualifies for alerting
4. **Alert**: A plain-text email is sent listing all newly stable files
5. **Purge**: Files that disappear between polls are removed from tracking and can re-alert if they reappear

## Project Structure

```
filewatcher-alerts/
├── FileWatcherAlerts.sln                  # Visual Studio solution
├── README.md                              # This file
├── CLAUDE.md                              # AI assistant development guidelines
│
├── FileWatcherAlerts/                     # Main application
│   ├── FileWatcherAlerts.csproj
│   ├── App.config                         # All configuration
│   ├── Program.cs                         # Entry point (console/service detection)
│   ├── FileWatcherService.cs              # Windows Service (ServiceBase)
│   ├── ProjectInstaller.cs                # InstallUtil service installer
│   ├── Configuration/
│   │   ├── WatcherConfigSection.cs        # Custom config section + SMTP element
│   │   ├── WatchedDirectoryElement.cs     # Per-directory config element
│   │   └── WatchedDirectoryCollection.cs  # Directory element collection
│   ├── Monitoring/
│   │   ├── TrackedFile.cs                 # File tracking data class
│   │   ├── StabilityTracker.cs            # In-memory stability state manager
│   │   └── FilePoller.cs                  # Polling orchestrator
│   ├── Alerting/
│   │   └── EmailAlertSender.cs            # SMTP email sender
│   └── Logging/
│       └── Log.cs                         # Trace + console logger
│
└── FileWatcherAlerts.Tests/               # Unit tests (MSTest)
    ├── FileWatcherAlerts.Tests.csproj
    ├── StabilityTrackerTests.cs           # Core stability logic tests
    ├── TrackedFileTests.cs                # Data class tests
    └── FilePollerTests.cs                 # Integration tests with real files
```

## Running Tests

Tests use MSTest. Run from Visual Studio Test Explorer, or via command line:

```cmd
vstest.console.exe FileWatcherAlerts.Tests\bin\Debug\FileWatcherAlerts.Tests.dll
```

Or with `dotnet test` if the .NET CLI is available:

```cmd
dotnet test FileWatcherAlerts.Tests\FileWatcherAlerts.Tests.csproj
```

## Windows Service Notes

- **Service name**: `FileWatcherAlerts`
- **Display name**: `File Watcher Alerts`
- **Default account**: `LocalSystem` (can be changed in `services.msc` after installation or by editing `ProjectInstaller.cs`)
- **Start type**: `Automatic` (starts on boot)
- The service runs the polling loop on a background thread and responds to stop requests within 30 seconds
- Configuration is read from `FileWatcherAlerts.exe.config` in the same directory as the executable

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Service fails to start | Check the log file and Windows Event Viewer for errors. Verify `App.config` is valid. |
| No alerts received | Verify SMTP settings. Check that the polling interval has elapsed and files meet the stability threshold. |
| "Cannot access directory" warnings | Ensure the service account has read access to the UNC paths. `LocalSystem` cannot access network shares by default — use a domain service account. |
| Duplicate alerts after restart | Expected behavior. In-memory tracking is lost on restart, so stable files are re-detected. |
| High memory usage | If monitoring directories with thousands of files, this is normal — each tracked file uses a small in-memory entry. |

## License

This project is provided as-is for internal use.
