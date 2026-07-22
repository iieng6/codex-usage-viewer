# Changelog

## [2.0.0] - 2026-07-19

### Added

- Edge-dot collapse mode with separate 12 px visuals and 20 px hit target
- Double-click collapse and single-click dot restoration
- Left/right screen-edge snapping after dragging
- Foreground fullscreen detection with automatic hide and state restoration
- Widget and system-tray controls for visibility, refresh, topmost mode, fullscreen behavior, language, and exit
- Simplified Chinese and English resources with a follow-system option and English fallback
- Persistent window, edge-dot, topmost, fullscreen, language, opacity, and hint settings
- One-time interaction hint
- Minimal successful-usage cache and expanded local diagnostics

### Changed

- Kept single-click assigned to usage refresh and last-update display
- Assigned collapse to double-click and explicit menu actions
- Kept the edge dot fully visible at the selected screen edge
- Preserved the left half for the 5-hour limit and the right half for the weekly limit
- Increased the expanded detail width so English status text and reset times fit
- Moved click, double-click, hover, and drag handling closer to native window messages

### Fixed

- Fixed conflicts between single-click, double-click, and drag gestures
- Fixed recovery of a hidden widget through the tray show action and tray-icon double-click
- Fixed edge-dot positioning so it remains inside screen bounds
- Improved monitor selection, visible-bound correction, and DPI-aware drag deltas
- Added a settings fallback when the normal AppData location cannot be written
- Removed executable paths, foreground window titles, and exception message bodies from local diagnostics
