using System.Threading;
using System.Threading.Tasks;
using Saber.Risk.Core.Models;
// Definiujemy alias, który jednoznacznie wskazuje na Twoją klasę generyczną
using PagedResultData = Saber.Risk.Core.Utils.PagedData<Saber.Risk.Core.Models.RiskMetricDto>;

namespace Saber.Risk.Core.Services
{
    public interface IRiskDataService
    {
        // Używamy aliasu zamiast nazwy PagedResult
        Task<PagedResultData> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default);

        Task<RiskMetricDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
