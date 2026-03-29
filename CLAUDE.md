# CLAUDE.md — filewatcher-alerts

## Project Overview

**filewatcher-alerts** is a .NET Framework 4.8 application that monitors Windows shared directories (UNC paths) and NAS folders for file availability. It can run as a **console application** or as a **Windows Service**. It polls configured directories on a timer, tracks file stability (file present and unchanged for a configurable duration), and sends email alerts when files become stable.

Repository: `pmangalapally/filewatcher-alerts`

## Repository Structure

```
filewatcher-alerts/
├── CLAUDE.md                              # AI assistant guidelines (this file)
├── FileWatcherAlerts.sln                  # Visual Studio solution file
└── FileWatcherAlerts/                     # Main project
    ├── FileWatcherAlerts.csproj           # Project file (.NET Framework 4.8)
    ├── App.config                         # Application configuration (directories, SMTP, polling)
    ├── Program.cs                         # Entry point — detects console vs service mode
    ├── FileWatcherService.cs              # ServiceBase implementation + console runner
    ├── ProjectInstaller.cs                # InstallUtil installer (service name, account, start type)
    ├── Configuration/
    │   ├── WatcherConfigSection.cs        # Custom ConfigurationSection + SmtpElement
    │   ├── WatchedDirectoryElement.cs     # Per-directory config (path, pattern, stability, recipients)
    │   └── WatchedDirectoryCollection.cs  # Collection of WatchedDirectoryElement
    ├── Monitoring/
    │   ├── TrackedFile.cs                 # Data class for a file being tracked
    │   ├── StabilityTracker.cs            # In-memory dictionary tracking file stability state
    │   └── FilePoller.cs                  # Orchestrates one polling cycle across all directories
    ├── Alerting/
    │   └── EmailAlertSender.cs            # Sends email alerts via System.Net.Mail.SmtpClient
    └── Logging/
        └── Log.cs                         # Static logging helper (Trace + Console)
```

## How It Works

1. **Polling**: `FilePoller` iterates configured directories every N seconds (configurable). Uses `Directory.EnumerateFiles` — not `FileSystemWatcher` — because FSW is unreliable on SMB/NAS paths.
2. **Stability tracking**: `StabilityTracker` maintains an in-memory dictionary of `TrackedFile` entries. A file's stability clock resets whenever its size or last-write-time changes between polls. Once a file has been unchanged for `stabilityMinutes`, it qualifies for alerting.
3. **Alerting**: `EmailAlertSender` sends a plain-text email listing all newly stable files to the configured recipients for that directory.
4. **Purging**: Files that disappear between polls are removed from tracking. If a file reappears, it starts a fresh stability clock and can trigger a new alert.

## Build & Run

```bash
# Build (Visual Studio or MSBuild)
msbuild FileWatcherAlerts.sln /p:Configuration=Release

# Run as console (for development/debugging)
FileWatcherAlerts\bin\Release\FileWatcherAlerts.exe
# Stop with Ctrl+C (graceful shutdown)
```

### Install as Windows Service

```cmd
:: Install (run from an elevated/Administrator command prompt)
C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe FileWatcherAlerts.exe

:: Start / Stop / Remove
net start FileWatcherAlerts
net stop FileWatcherAlerts
C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe /u FileWatcherAlerts.exe
```

The service runs as **LocalSystem** by default. To change the account, edit `ProjectInstaller.cs` or configure it via `services.msc` after installation. The service starts automatically on boot (`ServiceStartMode.Automatic`).

## Configuration (App.config)

All settings live in `FileWatcherAlerts/App.config`:

- **`<appSettings>`**: `PollingIntervalSeconds` (default 60), `LogFilePath` (default `filewatcher.log`)
- **`<fileWatcher><smtp>`**: SMTP host, port, SSL, credentials, from address
- **`<fileWatcher><directories>`**: One `<add>` per watched directory with:
  - `path` — UNC path (`\\server\share`) or mapped drive (`Z:\folder`)
  - `filePattern` — wildcard filter (default `*`)
  - `stabilityMinutes` — how long a file must be unchanged before alerting (use 60 for 1 hour, 1440 for 1 day)
  - `recipients` — semicolon-delimited email addresses

## Development Workflow

### Branch Strategy

- **Main branch:** `main`
- **Feature branches:** Prefix with `feature/`, `fix/`, `docs/`, `claude/`
- Open pull requests for review before merging to `main`

### Commit Conventions

- Imperative mood (e.g. "Add email retry logic", not "Added email retry logic")
- Keep commits focused on a single logical change
- Reference issue numbers where applicable (`Fix #12: handle locked files`)

## Key Conventions for AI Assistants

### General Guidelines

- **Read before writing:** Always read existing files before modifying them
- **Minimal changes:** Only change what is necessary; avoid unrelated refactoring
- **No speculative code:** Do not add features, abstractions, or error handling beyond what is requested
- **Security first:** Never introduce path traversal or injection vulnerabilities — this tool operates on filesystem paths from configuration
- **.NET Framework idioms:** Use `System.Configuration` for config, `System.Net.Mail` for email, `System.Diagnostics.Trace` for logging. No NuGet packages — the project is dependency-free by design.

### Code Style

- C# with .NET Framework 4.8 conventions
- `var` for obviously-typed local variables; explicit types when the type isn't clear from context
- Braces on new lines (Allman style)
- `StringComparer.OrdinalIgnoreCase` for all file path comparisons
- Exception filters with `when` for expected I/O errors; catch-and-log, don't crash

### File System Safety

- Never follow symlinks outside the intended watch scope
- Validate and sanitize all file paths configured in App.config
- Handle `IOException` and `UnauthorizedAccessException` gracefully — network shares go offline
- Use `Directory.EnumerateFiles` (streaming) instead of `Directory.GetFiles` (loads all into memory)

## Architecture Notes

- **No persistent state**: Tracking is in-memory only. On restart, already-stable files will be re-detected and re-alerted on the first poll cycle. This is acceptable for v1.
- **Dual-mode execution**: `Program.Main` checks `Environment.UserInteractive` — if true, runs as a console app with Ctrl+C shutdown; if false, runs via `ServiceBase.Run` as a Windows Service.
- **Single-threaded polling**: The main loop polls directories sequentially. For environments with many slow network paths, a future enhancement could poll directories in parallel.
- **No FileSystemWatcher**: Deliberate design decision. FSW relies on `ReadDirectoryChangesW` which does not reliably propagate over SMB/CIFS to NAS devices.

## Dependencies

None beyond .NET Framework 4.8 BCL:
- `System.Configuration` — custom config sections
- `System.Configuration.Install` — InstallUtil service installer
- `System.ServiceProcess` — Windows Service support (ServiceBase)
- `System.Net.Mail` — SMTP email sending
- `System.Diagnostics` — trace-based logging
- `System.IO` — file enumeration and metadata
