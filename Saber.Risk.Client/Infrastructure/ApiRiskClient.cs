using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Saber.Risk.Core.Models;
using Saber.Risk.Core.Utils;

namespace Saber.Risk.Client.Infrastructure
{
    /// <summary>
    /// Client for calling Saber.Risk.Api REST endpoints and subscribing to SignalR updates.
    /// </summary>
    /// // PL: Klient HTTP + SignalR do komunikacji z serwerowym API (paginacja + push updates).
    public class ApiRiskClient : IAsyncDisposable
    {
        private readonly HttpClient _http;
        private readonly string _hubUrl;
        private readonly JsonSerializerOptions _jsonOptions;
        private HubConnection? _hubConnection;
        private bool _connected;
        private string? _authToken; // Tu będziemy trzymać "klucz"
        /// <summary>
        /// Raised when a risk metric update is received from the server via SignalR.
        /// </summary>
        /// // PL: Zdarzenie wywoływane przy otrzymaniu aktualizacji metryki z serwera.
        public event Action<RiskMetricDto>? RiskUpdated;

        /// <summary>
        /// Creates a new instance of ApiRiskClient.
        /// </summary>
        /// <param name="http">HttpClient configured with base address of the API (e.g. http://localhost:5000/).</param>
        /// <param name="hubUrl">Relative or absolute URL of the SignalR hub (e.g. "/hubs/risk" or "http://localhost:5000/hubs/risk").</param>
        /// // PL: Konstruktor. 'http' to HttpClient z ustawionym BaseAddress, 'hubUrl' to adres hubu SignalR.
        public ApiRiskClient(HttpClient http, string hubUrl)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _hubUrl = hubUrl ?? throw new ArgumentNullException(nameof(hubUrl));
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        /// <summary>
        /// Requests a page from the API (server-side paging + optional search).
        /// </summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="search">Optional search string.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>PagedResult of RiskMetricDto.</returns>
        /// // PL: Pobiera stronę wyników z API. Zwraca PagedResult&lt;RiskMetricDto&gt;.
        public async Task<PagedData<RiskMetricDto>> GetPagedAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            var sb = new StringBuilder("api/risk?");
            sb.Append($"pageNumber={page}&pageSize={pageSize}");
            if (!string.IsNullOrWhiteSpace(search))
            {
                sb.Append("&search=");
                sb.Append(Uri.EscapeDataString(search));
            }

            using var resp = await _http.GetAsync(sb.ToString(), cancellationToken).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<PagedData<RiskMetricDto>>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
            return result ?? new PagedData<RiskMetricDto>();
        }

        public void SetAuthToken(string token)
        {
            // To jest "Seniorski" sposób dodawania tokena do nagłówka HTTP
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Connects to the SignalR hub and registers handlers.
        /// Safe to call multiple times; will no-op if already connected.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// // PL: Nawiązuje połączenie z SignalR i rejestruje handler do otrzymywania aktualizacji.
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_connected) return;

            // Build hub connection with full URL if necessary
            var url = _hubUrl;
            if (_http.BaseAddress != null && !_hubUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                // combine base address and relative hub path
                url = new Uri(_http.BaseAddress, _hubUrl).ToString();
            }

            _hubConnection = new HubConnectionBuilder()
    .WithUrl(url, options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(_authToken); // _authToken to pole, które ustawiasz w SetAuthToken
    })
    .WithAutomaticReconnect()
    .Build();

            // Register handler - server calls "ReceiveRiskUpdate"
            _hubConnection.On<RiskMetricDto>("ReceiveRiskUpdate", dto =>
            {
                try
                {
                    RiskUpdated?.Invoke(dto);
                }
                catch
                {
                    // swallow handler exceptions to avoid breaking SignalR loop
                }
            });

            await _hubConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            _connected = true;
        }

        /// <summary>
        /// Disconnects from the SignalR hub.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// // PL: Rozłącza klienta SignalR.
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!_connected || _hubConnection is null) return;

            try
            {
                await _hubConnection.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // ignore stop errors
            }

            // Dispose the hub connection using the appropriate mechanism available on the runtime.
            // Some SignalR client versions implement IAsyncDisposable, others only IDisposable.
            // We try IAsyncDisposable first, then fall back to IDisposable.
            try
            {
                if (_hubConnection is IAsyncDisposable asyncDisp)
                {
                    await asyncDisp.DisposeAsync().ConfigureAwait(false);
                }
                else if (_hubConnection is IDisposable syncDisp)
                {
                    syncDisp.Dispose();
                }
            }
            catch
            {
                // ignore dispose errors
            }
            finally
            {
                _hubConnection = null;
                _connected = false;
            }
        }

        /// <summary>
        /// Subscribes to ticker groups on the server (optional).
        /// Server hub is expected to expose 'Subscribe' and 'Unsubscribe' methods accepting string[].
        /// </summary>
        /// <param name="tickers">Tickers to subscribe to.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// // PL: Subskrybuje grupy tickerów na serwerze (jeżeli hub obsługuje Subscribe/Unsubscribe).
        public async Task SubscribeTickersAsync(IEnumerable<string> tickers, CancellationToken cancellationToken = default)
        {
            if (!_connected || _hubConnection is null) throw new InvalidOperationException("SignalR connection is not established.");
            var array = System.Linq.Enumerable.ToArray(tickers);
            if (array.Length == 0) return;
            await _hubConnection.InvokeAsync("Subscribe", array, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Unsubscribes from ticker groups on the server.
        /// </summary>
        /// <param name="tickers">Tickers to unsubscribe.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// // PL: Anuluje subskrypcje dla podanych tickerów.
        public async Task UnsubscribeTickersAsync(IEnumerable<string> tickers, CancellationToken cancellationToken = default)
        {
            if (!_connected || _hubConnection is null) throw new InvalidOperationException("SignalR connection is not established.");
            var array = System.Linq.Enumerable.ToArray(tickers);
            if (array.Length == 0) return;
            await _hubConnection.InvokeAsync("Unsubscribe", array, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes the client and releases resources.
        /// </summary>
        /// // PL: Zwalnia zasoby.
        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.StopAsync().ConfigureAwait(false);
                }
                catch { /* ignore */ }

                try
                {
                    if (_hubConnection is IAsyncDisposable asyncDisp)
                    {
                        await asyncDisp.DisposeAsync().ConfigureAwait(false);
                    }
                    else if (_hubConnection is IDisposable syncDisp)
                    {
                        syncDisp.Dispose();
                    }
                }
                catch { /* ignore */ }

                _hubConnection = null;
                _connected = false;
            }

            // Note: Do not dispose injected HttpClient; caller owns it.
            GC.SuppressFinalize(this);
        }
    }
}