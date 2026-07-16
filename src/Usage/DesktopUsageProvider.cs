using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodexUsageViewer.Usage
{
    internal sealed class DesktopUsageProvider : IUsageProvider
    {
        private const string InitializeRequest = "{\"method\":\"initialize\",\"id\":1,\"params\":{\"clientInfo\":{\"name\":\"codex-usage-viewer\",\"title\":\"Codex Usage Viewer\",\"version\":\"" + ProgramInfo.Version + "\"},\"capabilities\":{\"experimentalApi\":true}}}";
        private const string InitializedNotification = "{\"method\":\"initialized\",\"params\":{}}";
        private const string RateLimitsRequest = "{\"method\":\"account/rateLimits/read\",\"id\":2,\"params\":null}";

        public async Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken)
        {
            using (Process process = CreateProcess())
            {
                try
                {
                    if (!process.Start())
                    {
                        throw new UsageException(UsageError.AppServerUnavailable, "无法启动 Codex App Server。");
                    }
                }
                catch (UsageException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new UsageException(UsageError.AppServerUnavailable, "无法启动 Codex App Server。请确认已安装 Codex。", exception);
                }

                try
                {
                    await WriteLineAsync(process, InitializeRequest).ConfigureAwait(false);
                    await WaitForResponseAsync(process, 1, cancellationToken).ConfigureAwait(false);
                    await WriteLineAsync(process, InitializedNotification).ConfigureAwait(false);
                    await WriteLineAsync(process, RateLimitsRequest).ConfigureAwait(false);

                    string responseJson = await WaitForResponseAsync(process, 2, cancellationToken).ConfigureAwait(false);
                    return ParseResponse(responseJson);
                }
                finally
                {
                    StopProcess(process);
                }
            }
        }

        private static Process CreateProcess()
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"& codex app-server --stdio -c analytics.enabled=false\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false
                }
            };
        }

        private static async Task WriteLineAsync(Process process, string message)
        {
            await process.StandardInput.WriteLineAsync(message).ConfigureAwait(false);
            await process.StandardInput.FlushAsync().ConfigureAwait(false);
        }

        private static async Task<string> WaitForResponseAsync(Process process, int expectedId, CancellationToken cancellationToken)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(20);
            Task<string> readTask = process.StandardOutput.ReadLineAsync();

            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Task completedTask = await Task.WhenAny(readTask, Task.Delay(250, cancellationToken)).ConfigureAwait(false);

                if (completedTask != readTask)
                {
                    if (process.HasExited)
                    {
                        throw CreateProcessExitException(process);
                    }
                    continue;
                }

                string line = await readTask.ConfigureAwait(false);
                if (line == null)
                {
                    throw CreateProcessExitException(process);
                }

                ResponseEnvelope envelope = Deserialize<ResponseEnvelope>(line);
                if (envelope != null && envelope.Id == expectedId)
                {
                    if (envelope.Error != null)
                    {
                        throw ClassifyProtocolError(envelope.Error.Message);
                    }
                    return line;
                }

                readTask = process.StandardOutput.ReadLineAsync();
            }

            throw new UsageException(UsageError.AppServerUnavailable, "Codex App Server 响应超时。");
        }

        private static UsageSnapshot ParseResponse(string json)
        {
            RateLimitsEnvelope envelope;
            try
            {
                envelope = Deserialize<RateLimitsEnvelope>(json);
            }
            catch (Exception exception)
            {
                throw new UsageException(UsageError.ProtocolChanged, "OpenAI 可能已更新 App Server 返回结构。", exception);
            }

            if (envelope == null || envelope.Result == null || envelope.Result.RateLimits == null)
            {
                throw new UsageException(UsageError.ProtocolChanged, "OpenAI 可能已更新 App Server 返回结构。");
            }

            UsageWindow first = CreateWindow(envelope.Result.RateLimits.Primary);
            UsageWindow second = CreateWindow(envelope.Result.RateLimits.Secondary);
            UsageWindow shortWindow = null;
            UsageWindow longWindow = null;

            ClassifyWindow(first, ref shortWindow, ref longWindow);
            ClassifyWindow(second, ref shortWindow, ref longWindow);

            return new UsageSnapshot(shortWindow, longWindow);
        }

        private static UsageWindow CreateWindow(RateLimitWindow window)
        {
            if (window == null)
            {
                return null;
            }
            if (!window.UsedPercent.HasValue || !window.ResetsAt.HasValue || !window.WindowDurationMins.HasValue)
            {
                return null;
            }
            if (window.UsedPercent.Value < 0 || window.UsedPercent.Value > 100 || window.ResetsAt.Value <= 0 || window.WindowDurationMins.Value <= 0)
            {
                return null;
            }

            return new UsageWindow(window.UsedPercent.Value, window.ResetsAt.Value, window.WindowDurationMins.Value);
        }

        private static void ClassifyWindow(UsageWindow window, ref UsageWindow shortWindow, ref UsageWindow longWindow)
        {
            if (window == null)
            {
                return;
            }

            if (window.DurationMinutes < 1440 && window.DurationMinutes % 60 == 0)
            {
                shortWindow = window;
            }
            else if (window.DurationMinutes >= 1440 && window.DurationMinutes % 1440 == 0)
            {
                longWindow = window;
            }
        }

        private static T Deserialize<T>(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (MemoryStream stream = new MemoryStream(bytes, false))
            {
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
            }
        }

        private static UsageException CreateProcessExitException(Process process)
        {
            return new UsageException(UsageError.AppServerUnavailable, "无法连接 Codex App Server。");
        }

        private static UsageException ClassifyProtocolError(string message)
        {
            if (Contains(message, "unauthorized") || Contains(message, "not logged in") || Contains(message, "login"))
            {
                return new UsageException(UsageError.NotSignedIn, "未登录 ChatGPT。请先在 Codex 中登录。");
            }
            return new UsageException(UsageError.ProtocolChanged, "OpenAI 可能已更新 App Server 接口。");
        }

        private static bool Contains(string value, string text)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void StopProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(2000);
                }
            }
            catch
            {
            }
        }

        [DataContract]
        private sealed class ResponseEnvelope
        {
            [DataMember(Name = "id")]
            public int Id { get; set; }

            [DataMember(Name = "error")]
            public ProtocolError Error { get; set; }
        }

        [DataContract]
        private sealed class ProtocolError
        {
            [DataMember(Name = "message")]
            public string Message { get; set; }
        }

        [DataContract]
        private sealed class RateLimitsEnvelope
        {
            [DataMember(Name = "result")]
            public RateLimitsResult Result { get; set; }
        }

        [DataContract]
        private sealed class RateLimitsResult
        {
            [DataMember(Name = "rateLimits")]
            public RateLimitsPayload RateLimits { get; set; }
        }

        [DataContract]
        private sealed class RateLimitsPayload
        {
            [DataMember(Name = "primary")]
            public RateLimitWindow Primary { get; set; }

            [DataMember(Name = "secondary")]
            public RateLimitWindow Secondary { get; set; }
        }

        [DataContract]
        private sealed class RateLimitWindow
        {
            [DataMember(Name = "usedPercent")]
            public int? UsedPercent { get; set; }

            [DataMember(Name = "resetsAt")]
            public long? ResetsAt { get; set; }

            [DataMember(Name = "windowDurationMins")]
            public long? WindowDurationMins { get; set; }
        }
    }
}
