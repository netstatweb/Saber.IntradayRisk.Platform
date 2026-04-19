using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Saber.Risk.Client.Infrastructure
{
    /// <summary>
    /// Legacy client-side data service. Disabled — do NOT use directly from the WPF client.
    /// Use <see cref="ApiRiskClient"/> (Http + SignalR) or the server-side <c>IRiskDataService</c> exposed by the API.
    /// </summary>
    /// // PL: Legacy'owy serwis dostępu do bazy po stronie klienta. NIE używaj tego w WPF.
    /// // PL: Zamiast tego użyj ApiRiskClient (HTTP + SignalR) lub serwera (IRiskDataService).
    [Obsolete("Legacy client-side DB access removed. Use ApiRiskClient or server-side IRiskDataService via Saber.Risk.Api.", false)]
    public class RiskDataService
    {
        /// <summary>
        /// Constructor intentionally disabled to avoid accidental DB access from client.
        /// </summary>
        /// // PL: Konstruktor celowo blokuje tworzenie instancji żeby zapobiec bezpośrednim połączeniom do DB z klienta.
        public RiskDataService()
        {
            // Fail fast at runtime with an explicit message so accidental usage is obvious during testing.
            throw new NotSupportedException("Legacy RiskDataService is disabled in the client. Use ApiRiskClient (Http + SignalR) or call the server IRiskDataService.");
        }

        /// <summary>
        /// Legacy signature kept for compatibility, but it always throws.
        /// </summary>
        /// // PL: Sygnatura pozostawiona dla kompatybilności, metoda zawsze rzuca wyjątek.
        //public Task<PagedResult<Models.RiskMetric>> GetPagedAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default)
        //{
        //    throw new NotSupportedException("Legacy RiskDataService.GetPagedAsync is disabled. Use ApiRiskClient.GetPagedAsync instead.");
        //}

        /// <summary>
        /// Legacy method kept for compatibility; disabled.
        /// </summary>
        /// // PL: Metoda legacy — wyłączona.
        public Task<List<Models.RiskMetric>> GetInitialSnapshotAsync()
        {
            throw new NotSupportedException("Legacy RiskDataService.GetInitialSnapshotAsync is disabled. Use the API for data retrieval.");
        }
    }

    /// <summary>
    /// Local PagedResult kept for backward compatibility with client code referencing it.
    /// Prefer using Saber.Risk.Core.Utils.PagedResult from the shared core.
    /// </summary>
    /// // PL: Lokalny PagedResult zachowany dla kompatybilności. Preferuj Saber.Risk.Core.Utils.PagedResult z Core.
   
}