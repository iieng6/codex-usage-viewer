using System;

namespace CodexUsageViewer.Usage
{
    internal enum UsageError
    {
        NotSignedIn,
        AppServerUnavailable,
        ProtocolChanged,
        InvalidData
    }

    internal sealed class UsageException : Exception
    {
        public UsageException(UsageError error, string message)
            : base(message)
        {
            Error = error;
        }

        public UsageException(UsageError error, string message, Exception innerException)
            : base(message, innerException)
        {
            Error = error;
        }

        public UsageError Error { get; private set; }
    }
}
