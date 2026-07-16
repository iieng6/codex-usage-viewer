using System.Threading;
using System.Threading.Tasks;

namespace CodexUsageViewer.Usage
{
    internal sealed class UsageService
    {
        private readonly IUsageProvider provider;

        public UsageService(IUsageProvider provider)
        {
            this.provider = provider;
        }

        public Task<UsageSnapshot> RefreshAsync(CancellationToken cancellationToken)
        {
            return provider.GetUsageAsync(cancellationToken);
        }
    }
}
