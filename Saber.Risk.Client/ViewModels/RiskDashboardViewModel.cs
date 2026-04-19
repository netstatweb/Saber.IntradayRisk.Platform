using Saber.Risk.Client.Core;
using Saber.Risk.Client.Infrastructure;
using Saber.Risk.Client.Models;
using Saber.Risk.Core.Models;
using Saber.Risk.Core.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Saber.Risk.Client.ViewModels
{
    /// <summary>
    /// ViewModel dla głównego dashboardu ryzyka. 
    /// Zarządza danymi pobieranymi przez REST oraz aktualizacjami Real-Time przez SignalR.
    /// </summary>
    public class RiskDashboardViewModel : ViewModelBase
    {
        private readonly ApiRiskClient _apiClient;
        private CancellationTokenSource? _debounceCts;
        private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(300);

        // Kolekcja zbindowana do DataGrid w WPF
        public ObservableCollection<RiskMetric> RiskPositions { get; } = new ObservableCollection<RiskMetric>();

        #region Properties (Paginacja i Wyszukiwanie)

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery == value) return;
                _searchQuery = value;
                OnPropertyChanged();
                DebounceAndLoadPage();
            }
        }

        private int _pageSize = 50;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize == value) return;
                _pageSize = Math.Max(1, value);
                OnPropertyChanged();
                _ = LoadPageAsync(1, CancellationToken.None);
            }
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                var newVal = Math.Max(1, value);
                if (_currentPage == newVal) return;
                _currentPage = newVal;
                OnPropertyChanged();
            }
        }

        private int _totalItems;
        public int TotalItems
        {
            get => _totalItems;
            private set { _totalItems = value; OnPropertyChanged(); }
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            private set { _totalPages = value; OnPropertyChanged(); }
        }

        #endregion

        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public string ThemeButtonText => _isDarkMode ? "LIGHT MODE" : "DARK MODE";
        public string ThemeButtonIcon => _isDarkMode ? "☀️" : "🌙";
        public RiskDashboardViewModel()
        {
            // 1. Konfiguracja klienta (Adres zgodny z Twoim API HTTPS)
            var http = new HttpClient { BaseAddress = new Uri("https://localhost:7240/") };
            _apiClient = new ApiRiskClient(http, "/hubs/risk");

            // 2. Rejestracja zdarzenia aktualizacji z SignalR
            _apiClient.RiskUpdated += OnRiskUpdated;

            // 3. Komendy paginacji
            NextPageCommand = new RelayCommand(_ => { _ = ChangePageAsync(CurrentPage + 1); }, _ => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(_ => { _ = ChangePageAsync(CurrentPage - 1); }, _ => CurrentPage > 1);

            // 4. ToggleThemeCommand:
            ToggleThemeCommand = new RelayCommand(_ => SwitchTheme());

            // 5. Inicjalizacja danych i połączenia
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadPageAsync(1, CancellationToken.None);
            await _apiClient.ConnectAsync();

            // Opcjonalnie: Subskrypcja tickerów widocznych na pierwszej stronie
            var tickers = RiskPositions.Select(r => r.Ticker).ToArray();
            await _apiClient.SubscribeTickersAsync(tickers);
        }

        public void SetToken(string token)
        {
            _apiClient.SetAuthToken(token);
            // Teraz, gdy mamy token, możemy bezpiecznie pobrać dane
            _ = InitializeAsync();
        }

        /// <summary>
        /// Obsługa aktualizacji "na żywo" wysyłanych przez serwerowy MarketSimulator.
        /// </summary>
        private void OnRiskUpdated(RiskMetricDto dto)
        {
            if (dto == null) return;

            // Operacje na UI muszą być w Dispatcherze
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Szukamy elementu na aktualnej stronie po Tickerze
                var existing = RiskPositions.FirstOrDefault(p =>
                    string.Equals(p.Ticker, dto.Ticker, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // Aktualizujemy tylko wartości ryzyka. 
                    // Dzięki INotifyPropertyChanged w RiskMetric, komórki w DataGrid same zamigają.
                    existing.Delta = dto.Delta;
                    existing.Gamma = dto.Gamma;
                    existing.Vega = dto.Vega;
                    existing.Exposure = dto.Exposure;
                }
            });
        }

        private async Task LoadPageAsync(int pageNumber, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _apiClient.GetPagedAsync(pageNumber, PageSize, _searchQuery, cancellationToken);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TotalItems = result.TotalCount;
                    TotalPages = Math.Max(1, (int)Math.Ceiling(result.TotalCount / (double)PageSize));
                    CurrentPage = pageNumber;

                    RiskPositions.Clear();
                    foreach (var dto in result.Items)
                        RiskPositions.Add(MapDtoToModel(dto));
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Load error: {ex.Message}"); }
        }

        private void DebounceAndLoadPage()
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_debounceDelay, token);
                    await LoadPageAsync(1, token);
                }
                catch (OperationCanceledException) { }
            }, token);
        }

        private async Task ChangePageAsync(int newPage)
        {
            var targetPage = Math.Max(1, Math.Min(newPage, TotalPages));
            await LoadPageAsync(targetPage, CancellationToken.None);
        }

        private RiskMetric MapDtoToModel(RiskMetricDto dto)
        {
            return new RiskMetric
            {
                Ticker = dto.Ticker,
                Book = dto.Book,
                Currency = dto.Currency,
                Delta = dto.Delta,
                Gamma = dto.Gamma,
                Vega = dto.Vega,
                Exposure = dto.Exposure
            };
        }

        private bool _isDarkMode = false;
        public ICommand ToggleThemeCommand { get; }
        private void SwitchTheme()
        {
            var appRes = Application.Current.Resources;
            var themeKey = _isDarkMode ? "LightMode" : "DarkMode";

            if (appRes[themeKey] is ResourceDictionary newTheme)
            {
                appRes.MergedDictionaries.Clear();
                appRes.MergedDictionaries.Add(newTheme);

                _isDarkMode = !_isDarkMode;

                // POWIADAMIAMY UI O ZMIANIE TEKSTU NA PRZYCISKU
                OnPropertyChanged(nameof(ThemeButtonText));
                OnPropertyChanged(nameof(ThemeButtonIcon));
            }
        }
    }
}