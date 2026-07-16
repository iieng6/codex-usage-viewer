using System;
using System.IO;
using System.Text;

namespace CodexUsageViewer
{
    internal static class ProgramNetworkAudit
    {
        private const string Report =
            "Program Network Audit\r\n" +
            "\r\n" +
            "Program Name: Codex Usage Viewer\r\n" +
            "Version: v" + ProgramInfo.Version + "\r\n" +
            "Scope: CodexUsageViewer.exe only. The official codex app-server is a separate component.\r\n" +
            "\r\n" +
            "Program Requests: Yes - Sends only fixed local JSON-RPC initialization messages and account/rateLimits/read to the official codex app-server over redirected standard input/output.\r\n" +
            "Program Upload: No - Contains no internet upload implementation and sends no user data to a network address.\r\n" +
            "Program Downloads: No - Contains no download implementation. It receives the requested rate-limit response from the local official codex app-server process.\r\n" +
            "Third-party Requests: No - Contains no HTTP client, socket, third-party URL, or third-party SDK.\r\n" +
            "Cookie Access: No - Contains no browser, cookie store, or cookie API access.\r\n" +
            "Token Access: No - Contains no API key, access token, refresh token, credential store, or authentication-file access. Authentication remains inside the official codex app-server.\r\n" +
            "Clipboard Access: No - Contains no clipboard API access.\r\n" +
            "File Access: Yes - Reads and writes only its window geometry configuration and writes this static audit document. It does not read user documents or runtime Usage data from files.\r\n" +
            "Telemetry: No - Contains no telemetry, logging, crash-reporting, or event-reporting implementation.\r\n" +
            "Analytics: No - Contains no analytics implementation and explicitly starts codex app-server with analytics.enabled=false.\r\n" +
            "Data Persistence: Yes - Persists only window position/size configuration and this static program-capability audit. Usage, raw JSON, cookies, tokens, login state, chats, prompts, user identity, and runtime history are never persisted.\r\n";

        public static string Write()
        {
            string directoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CodexUsageViewer");
            string filePath = Path.Combine(directoryPath, "Program Network Audit.txt");
            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(filePath, Report, new UTF8Encoding(false));
            return filePath;
        }
    }
}
