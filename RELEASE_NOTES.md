## Codex Usage Viewer v2.0.0

Codex Usage Viewer is a lightweight, local-first Windows widget for checking your remaining Codex 5-hour and weekly allowances. Version 2.0.0 introduces a refined edge-friendly interface, clearer usage details, and more reliable desktop behavior.

<p align="center">
  <img src="https://raw.githubusercontent.com/iieng6/codex-usage-viewer/v2.0.0/docs/screenshots/widget-expanded.png" alt="Codex Usage Viewer showing reset times and update status" width="474">
</p>

### Highlights

- Compact two-color widget with remaining percentages, reset times, and freshness details
- Small two-color edge-dot mode that stays visible at the selected screen edge
- Drag-to-snap positioning, fullscreen auto-hide, and system tray controls
- Green, amber, and red allowance states for quick recognition
- English, Simplified Chinese, and follow-system language modes
- Saved window and interaction preferences, minimal local cache, and rotating diagnostics

### Controls

- **Hover:** Expand the widget and reveal reset and update details
- **Single-click:** Refresh usage data and display the last update time
- **Double-click:** Collapse the widget into the edge dot
- **Drag:** Move the widget and snap it to the nearest screen edge
- **Right-click:** Open the widget menu
- **Tray icon:** Show, hide, expand, collapse, refresh, change settings or language, and exit

### Download

Download `CodexUsageViewer.exe` below and run it directly. No installation or administrator privileges are required.

The application is currently unsigned, so Windows SmartScreen may display a warning. It requires Windows 10 or 11, the official `codex` command on `PATH`, and a signed-in Codex environment that can use `codex app-server`.

### Privacy

The viewer requests rate-limit data through the official local `codex app-server`. It has no direct HTTP client, telemetry, analytics, automatic updater, or third-party endpoint. Settings, display-ready cache values, and local rotating logs are stored under `%LOCALAPPDATA%\CodexUsageViewer`.

### SHA-256

```text
8DF33516F323490171421E17728682C5090A06A841442D8F85E56EB5E1C061AF  CodexUsageViewer.exe
```

### Notes

- Windows only
- Fullscreen auto-hide may vary with unusual overlay or borderless fullscreen applications
- Internal development builds used other non-public version numbers; v2.0.0 is the official public release

<details>
<summary><strong>中文发布说明</strong></summary>

Codex Usage Viewer 是一个轻量、本地优先的 Windows 悬浮组件，用于查看 Codex 五小时额度和周额度的剩余情况。v2.0.0 带来了更精致的靠边界面、更清晰的额度详情，以及更可靠的桌面交互。

#### 主要更新

- 双色紧凑组件，显示剩余比例、重置时间和数据更新时间
- 可折叠为始终可见的双色靠边圆点
- 支持拖动吸附屏幕边缘、全屏自动隐藏和系统托盘控制
- 使用绿色、琥珀色和红色快速区分额度状态
- 支持英文、简体中文和跟随系统语言
- 保存窗口与交互设置，并提供最小本地缓存和轮转诊断日志

#### 下载与要求

下载下方的 `CodexUsageViewer.exe` 后直接运行，无需安装或管理员权限。

程序目前没有数字签名，因此 Windows SmartScreen 可能显示警告。运行环境需要 Windows 10 或 Windows 11、可通过 `PATH` 调用的官方 `codex` 命令，以及能够使用 `codex app-server` 的有效登录环境。

本程序通过官方本地 `codex app-server` 获取额度信息，不包含直接 HTTP 客户端、遥测、分析、自动更新或第三方服务端点。

</details>
