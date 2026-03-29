# CLAUDE.md — filewatcher-alerts

## Project Overview

**filewatcher-alerts** is a file-monitoring and alerting system. The repository is currently in its initial setup phase with no source code yet committed.

Repository: `pmangalapally/filewatcher-alerts`

## Repository Structure

```
filewatcher-alerts/
├── CLAUDE.md          # This file — AI assistant guidelines
└── (empty)            # Project is newly initialized
```

> **Note:** This file should be updated as the project grows to reflect the actual codebase structure, modules, and conventions.

## Development Workflow

### Branch Strategy

- **Main branch:** `main`
- **Feature branches:** Use descriptive names prefixed with the type of work, e.g. `feature/`, `fix/`, `docs/`, `claude/`
- Always develop on feature branches and open pull requests for review before merging to `main`

### Getting Started

```bash
git clone https://github.com/pmangalapally/filewatcher-alerts.git
cd filewatcher-alerts
# Install dependencies (update once a package manager is chosen)
```

### Commit Conventions

- Write clear, concise commit messages in imperative mood (e.g. "Add file watcher module", not "Added file watcher module")
- Keep commits focused on a single logical change
- Reference issue numbers where applicable (e.g. `Fix #12: handle symlink detection`)

## Key Conventions for AI Assistants

### General Guidelines

- **Read before writing:** Always read existing files before modifying them
- **Minimal changes:** Only change what is necessary to complete the task; avoid unrelated refactoring
- **No speculative code:** Do not add features, abstractions, or error handling beyond what is requested
- **Security first:** Never introduce command injection, path traversal, or other vulnerabilities — especially important for a file-watching tool that interacts with the filesystem
- **Test your changes:** Run the project's test suite after making changes (update this section once tests are configured)

### Code Style

- Follow the existing code style and conventions established in the project
- Use the project's configured linter and formatter (to be set up)
- Prefer clarity over cleverness

### File System Safety

Since this project monitors files and directories:

- Never follow symlinks outside the intended watch scope without explicit configuration
- Validate and sanitize all file paths
- Be cautious with recursive directory watching to avoid performance issues
- Handle file permission errors gracefully

## Build & Test Commands

> **TODO:** Update this section as tooling is added to the project.

```bash
# (placeholder — update when build system is configured)
# npm install / pip install / go build / etc.
# npm test / pytest / go test ./... / etc.
# npm run lint / etc.
```

## CI/CD

> **TODO:** Update this section when CI/CD pipelines are configured (e.g. GitHub Actions).

## Dependencies

> **TODO:** Document key dependencies and their purposes once they are added.

## Architecture Notes

> **TODO:** Document the system architecture, core modules, and data flow once implementation begins. Expected components may include:
>
> - **File watcher:** Monitors filesystem events (create, modify, delete, rename)
> - **Alert dispatcher:** Routes alerts to configured channels (email, Slack, webhook, etc.)
> - **Configuration manager:** Handles watch rules, filters, and alert preferences
