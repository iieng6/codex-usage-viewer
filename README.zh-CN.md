# Codex Usage Viewer

[English](README.md) | 中文

一个轻量的 Windows 悬浮组件，通过官方本地 `codex app-server` 显示 Codex 五小时额度和周额度的剩余情况。

## 界面预览

<p align="center">
  <img src="docs/screenshots/widget-expanded.png" alt="Codex Usage Viewer 展开后显示重置时间和更新状态" width="474">
</p>

紧凑组件会在鼠标悬停时展开，显示额度重置时间和数据更新时间。无操作四秒后，组件会柔和地降低至 80% 不透明度，在保持可见的同时减少干扰。

| 默认待机状态 | 靠边圆点模式 | 组件菜单 |
| --- | --- | --- |
| <img src="docs/screenshots/widget-idle.png" alt="降低不透明度后的待机状态" width="222"> | <img src="docs/screenshots/edge-dot.png" alt="吸附在屏幕边缘的双色圆点" width="60"> | <img src="docs/screenshots/context-menu.png" alt="组件右键菜单" width="280"> |

### 额度颜色

| 充足 | 警告 | 较低 |
| --- | --- | --- |
| <img src="docs/screenshots/status-healthy.png" alt="绿色表示额度充足" width="223"> | <img src="docs/screenshots/status-warning.png" alt="琥珀色表示额度进入警告范围" width="223"> | <img src="docs/screenshots/status-low.png" alt="红色表示剩余额度较低" width="223"> |

## 功能

- 分别显示五小时额度和周额度的剩余比例与颜色状态
- 显示重置日期、时间及最近更新时间
- 支持始终置顶的紧凑药丸形态和悬停展开动画
- 支持双色靠边圆点，并使用更大的透明点击热区
- 区分单击、双击和拖动，拖动后吸附最近的左右屏幕边缘
- 支持全屏自动隐藏和系统托盘控制
- 支持 English、简体中文和跟随系统语言
- 持久化窗口位置、圆点状态、吸附边缘、透明度、置顶、全屏隐藏、语言和提示状态
- 支持启动刷新、自动刷新、单击刷新和托盘手动刷新，并保存最小额度缓存
- 提供本地轮转日志，不包含遥测或自动更新

## 系统要求

- Windows 10 或 Windows 11
- Windows PowerShell 5.1
- Windows .NET Framework 4.x WPF 组件
- 官方 `codex` 命令可通过 `PATH` 调用
- `codex app-server` 可以使用的有效登录环境

## 下载

从 [v2.0.0 Release 页面](https://github.com/iieng6/codex-usage-viewer/releases/tag/v2.0.0) 下载 `CodexUsageViewer.exe`。

程序是免安装便携单文件。目前没有代码签名，Windows SmartScreen 可能显示警告。

## 使用方法

运行 `CodexUsageViewer.exe`。程序会先读取上次成功缓存（如存在），并在启动时请求最新额度，之后每 60 秒自动刷新。

显示比例为 `100 - usedPercent`。剩余至少 50% 为绿色，20～49% 为琥珀色，低于 20% 为红色，0% 为白色，数据不可用为灰色。

## 操作

- 鼠标悬停完整组件：展开并显示重置详情。
- 单击完整组件：刷新额度并显示最近更新时间。
- 双击完整组件：折叠为靠边小圆点。
- 单击小圆点：恢复完整组件。
- 拖动组件：调整位置，松开后吸附最近的屏幕边缘。
- 右键组件或托盘图标：打开菜单。
- 使用托盘菜单或双击托盘图标：恢复已隐藏的组件。
- 通过托盘菜单切换语言。

菜单提供显示、展开/折叠、隐藏到托盘、始终置顶、全屏自动隐藏、立即刷新、语言和退出操作。

## 语言切换

在组件或托盘菜单中选择 **语言**：

- **跟随系统**：Windows UI culture 为 `zh-CN`、`zh-SG` 或 `zh-Hans-*` 时使用简体中文，其他语言使用英文。
- **简体中文**：始终使用简体中文。
- **English**：始终使用英文。

手动选择会立即生效并持久化。中文资源缺失时回退到英文。

## 数据与隐私

程序启动以下官方本地命令：

```text
codex app-server --stdio -c analytics.enabled=false
```

程序通过重定向的标准输入输出发送固定初始化消息和 `account/rateLimits/read`，只读取额度比例、重置时间戳、窗口时长、用于匹配的请求 ID 和协议错误状态。本程序不包含 HTTP 客户端、第三方服务、遥测、分析或自动更新。

本地文件保存在 `%LOCALAPPDATA%\CodexUsageViewer`：

- `window-state.txt`：窗口和交互设置
- `usage-cache.json`：用于显示的比例、重置时间和上次成功更新时间
- `CodexUsageViewer.log`：本地轮转诊断日志
- `Program Network Audit.txt`：静态隐私与网络审计说明

身份验证由官方 `codex app-server` 处理。本程序不会持久化原始响应、Cookie、Token、认证头、提示词、对话或身份信息。详见 [SECURITY.md](SECURITY.md)。

## 故障排查

- 确认 `codex` 已加入 `PATH`，并能启动 `codex app-server`。
- 确认已通过官方 Codex 环境登录相应账号。
- 从组件或托盘菜单选择“立即刷新”。
- 如果组件已隐藏，选择托盘菜单“显示”或双击托盘图标。
- 查看 `%LOCALAPPDATA%\CodexUsageViewer\CodexUsageViewer.log`。
- 使用特殊覆盖层或无边框窗口的程序可能影响全屏自动隐藏判断。

## 从源码构建

项目采用轻量的 .NET Framework 构建脚本，不包含 `.sln` 或 `.csproj`。

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
powershell -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```

构建脚本调用 Windows .NET Framework C# 编译器，输出 `dist\CodexUsageViewer.exe`。

## 已知限制

- 仅支持 Windows。
- EXE 尚未签名。
- 需要官方 `codex` 命令和有效登录环境。
- 全屏检测基于前台窗口是否覆盖显示器，特殊全屏程序可能无法识别。
- 不包含安装程序或自动更新。

## 许可证

项目采用 [MIT License](LICENSE)。

本项目是独立开源工具，与 OpenAI 无关联，未经 OpenAI 认可，也不由 OpenAI 维护。
