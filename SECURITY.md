# Security and Privacy

## Release Status

Version `v2.0.0` is audited here as a local, read-only Codex Usage viewer.

This document distinguishes between two independent components:

- **This program:** `CodexUsageViewer.exe`
- **Official component:** `codex app-server`

This program communicates with the official App Server through redirected standard input and output. It does not contain direct network code.

## Data Read by This Program

The only business method is:

```text
account/rateLimits/read
```

The typed response model reads only:

- `primary.usedPercent`
- `primary.resetsAt`
- `primary.windowDurationMins`
- Whether `secondary` exists
- `secondary.usedPercent`, when present
- `secondary.resetsAt`, when present
- `secondary.windowDurationMins`, when present

The protocol layer additionally reads the response request ID and error state so it can match requests and report failures. The App Server standard error stream is not read.

All other response fields are ignored and are not stored, displayed, logged, or used.

## Data Not Accessed

This program does not access:

- Chat content, prompts, conversations, or usage history
- User names, email addresses, account profiles, or identity data
- API keys, access tokens, refresh tokens, or login state
- Cookies, browser history, browser caches, or browser passwords
- Windows Credential Manager, the registry, or the clipboard
- Documents, Desktop, Downloads, images, or unrelated user files

Authentication is handled entirely by the official `codex app-server`.

## Network Boundary

This program contains no HTTP client, web request API, socket, OpenAI URL, third-party URL, or third-party SDK.

It starts the official `codex app-server` and sends only fixed initialization messages plus `account/rateLimits/read` through local redirected standard input/output. The official App Server may access OpenAI services for its own operation; those requests belong to the official component. This program does not request, parse, or use unrelated App Server methods or data.

## Persistence

Only display-ready allowance values are persisted so a failed refresh can keep showing the last successful data. Raw responses and authentication material are never persisted.

The only persisted files are program configuration and documentation:

- `%LOCALAPPDATA%\CodexUsageViewer\window-state.txt`
- `%LOCALAPPDATA%\CodexUsageViewer\usage-cache.json`
- `%LOCALAPPDATA%\CodexUsageViewer\CodexUsageViewer.log` (rotated at 512 KiB, one backup)
- `%LOCALAPPDATA%\CodexUsageViewer\Program Network Audit.txt`

The JSON cache contains only two percentages, their reset times, a schema version, and the last successful update time. Logs contain categories and sanitized exception summaries, never tokens, cookies, authorization headers, raw responses, or account details.

## Permissions

This program does not request administrator privileges, modify the registry, create services or scheduled tasks, configure startup, inject processes, modify ChatGPT Desktop, or create file associations.

## Telemetry and Analytics

This program has no telemetry, analytics, remote logging, crash upload, or event-reporting implementation. Local rotating diagnostics are kept for troubleshooting. It explicitly starts the official App Server with `analytics.enabled=false`.

## Reporting a Security Issue

Please open a GitHub security advisory or contact the repository maintainer privately. Do not include cookies, tokens, raw App Server responses, account details, or other sensitive information in a public issue.
