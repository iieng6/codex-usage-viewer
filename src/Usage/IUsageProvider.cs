using System.Threading;
using System.Threading.Tasks;

namespace CodexUsageViewer.Usage
{
    internal interface IUsageProvider
    {
        Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken);
    }
}
