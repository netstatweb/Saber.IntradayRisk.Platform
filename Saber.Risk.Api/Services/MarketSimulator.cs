using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection; // Ważne dla CreateScope
using Saber.Risk.Api.Hubs;
using Saber.Risk.Core.Models;
using Saber.Risk.Core.Services;
using Saber.Risk.Core.Utils;

namespace Saber.Risk.Api.Services
{
    /// <summary>
    /// Background simulator that periodically produces synthetic market movements
    /// and pushes updates to connected SignalR clients via <see cref="RiskHub"/>.
    /// </summary>
    public class MarketSimulator : BackgroundService
    {
        private readonly IHubContext<RiskHub> _hub;
        private readonly IServiceScopeFactory _scopeFactory; // Używamy fabryki zamiast bezpośredniego serwisu
        private readonly ILogger<MarketSimulator> _logger;
        private readonly TimeSpan _interval;
        private readonly int _percentRowsToChange;
        private readonly double _maxDelta;
        private readonly Random _rand = new();

        public MarketSimulator(
            IHubContext<RiskHub> hub,
            IServiceScopeFactory scopeFactory, // Wstrzykujemy fabrykę scope'ów
            IConfiguration configuration,
            ILogger<MarketSimulator> logger)
        {
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var intervalMs = configuration.GetValue<int?>("MarketSimulator:IntervalMs") ?? 1000;
            _interval = TimeSpan.FromMilliseconds(Math.Max(100, intervalMs));

            _percentRowsToChange = Math.Clamp(configuration.GetValue<int?>("MarketSimulator:PercentRows") ?? 2, 1, 100);
            _maxDelta = Math.Max(0.0001, configuration.GetValue<double?>("MarketSimulator:MaxDelta") ?? 1.0);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MarketSimulator starting with interval {Interval} ms, percentRows {PercentRows}, maxDelta {MaxDelta}.",
                _interval.TotalMilliseconds, _percentRowsToChange, _maxDelta);

            const int fetchPageSize = 500;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Ręcznie tworzymy scope, aby móc użyć Scoped IRiskDataService
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dataService = scope.ServiceProvider.GetRequiredService<IRiskDataService>();

                        // Pobieramy dane z bazy
                        PagedData<RiskMetricDto> page = await dataService
                            .GetPagedAsync(1, fetchPageSize, null, stoppingToken)
                            .ConfigureAwait(false);

                        var items = page.Items;
                        if (items != null && items.Count > 0)
                        {
                            var toChange = Math.Max(1, (int)Math.Ceiling(items.Count * (_percentRowsToChange / 100.0)));
                            var indices = PickRandomIndices(items.Count, toChange);
                            var updates = new List<RiskMetricDto>(toChange);

                            foreach (var idx in indices)
                            {
                                var dto = items[idx];

                                // Symulacja ruchów (Random Walk)
                                dto.Delta += (_rand.NextDouble() * 2.0 - 1.0) * _maxDelta;
                                dto.Gamma += (_rand.NextDouble() * 2.0 - 1.0) * (_maxDelta * 0.1);
                                dto.Vega += (_rand.NextDouble() * 2.0 - 1.0) * (_maxDelta * 0.2);
                                dto.Exposure += (_rand.NextDouble() * 2.0 - 1.0) * (_maxDelta * 1000.0);
                                dto.LastUpdatedUtc = DateTime.UtcNow;

                                updates.Add(dto);
                            }

                            // Rozsyłanie aktualizacji przez SignalR
                            foreach (var update in updates)
                            {
                                var groupName = $"TICKER:{update.Ticker}";

                                // Do subskrybentów konkretnego tickera
                                await _hub.Clients.Group(groupName)
                                    .SendAsync("ReceiveRiskUpdate", update, cancellationToken: stoppingToken)
                                    .ConfigureAwait(false);

                                // Do wszystkich (dla uproszczenia widoku głównego)
                                await _hub.Clients.All
                                    .SendAsync("ReceiveRiskUpdate", update, cancellationToken: stoppingToken)
                                    .ConfigureAwait(false);
                            }

                            _logger.LogDebug("MarketSimulator: broadcasted {Count} updates.", updates.Count);
                        }
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.LogError(ex, "MarketSimulator caught unexpected exception.");
                }

                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
            }

            _logger.LogInformation("MarketSimulator stopping.");
        }

        private IReadOnlyList<int> PickRandomIndices(int n, int k)
        {
            k = Math.Min(k, n);
            var result = new List<int>(k);
            if (k * 4 < n)
            {
                var chosen = new HashSet<int>();
                while (chosen.Count < k) { chosen.Add(_rand.Next(0, n)); }
                result.AddRange(chosen);
                return result;
            }
            var arr = Enumerable.Range(0, n).ToArray();
            for (int i = 0; i < k; i++)
            {
                int j = _rand.Next(i, n);
                var tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
                result.Add(arr[i]);
            }
            return result;
        }
    }
}