using Microsoft.AspNetCore.Mvc;
using Saber.Risk.Core.Models;
using Saber.Risk.Core.Services;
using Saber.Risk.Core.Utils;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Saber.Risk.Api.Controllers
{
    /// <summary>
    /// REST API for retrieving risk metrics (paged).
    /// </summary>
    /// // PL: Kontroler REST zwracający paginowane dane ryzyka.
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RiskController : ControllerBase
    {
        private readonly IRiskDataService _service;

        /// <summary>
        /// Constructor with injected data service.
        /// </summary>
        /// // PL: Konstruktor z wstrzykniętym serwisem dostępu do danych.
        public RiskController(IRiskDataService service)
        {
            _service = service;
        }

        /// <summary>
        /// Returns a single page of risk metrics and the total matching count.
        /// </summary>
        /// <param name="page">1-based page number (default 1).</param>
        /// <param name="pageSize">Number of items per page (default 50).</param>
        /// <param name="search">Optional free-text search applied across columns.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>PagedResult containing items and total count.</returns>
        /// // PL: Zwraca jedną stronę danych oraz łączną liczbę elementów pasujących do filtra.
        [HttpGet]
        public async Task<ActionResult<PagedData<RiskMetricDto>>> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            var result = await _service.GetPagedAsync(page, pageSize, search, cancellationToken).ConfigureAwait(false);

            return Ok(result);
        }
    }
}