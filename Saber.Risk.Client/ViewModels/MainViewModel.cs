using Saber.Risk.Client;
using Saber.Risk.Client.Core;
using Saber.Risk.Client.Infrastructure;
using Saber.Risk.Client.Models;
using System.Collections.ObjectModel;

public partial class MainViewModel : ViewModelBase
{
    private readonly RiskDataService _dataService;
    private readonly Dictionary<string, RiskMetric> _positionsCache; // Szybki dostęp do pozycji po tickerze
    public ObservableCollection<RiskMetric> RiskPositions { get; set; }
    public MainViewModel()
    {
        _dataService = new RiskDataService();
        _positionsCache = new Dictionary<string, RiskMetric>();
        RiskPositions = new ObservableCollection<RiskMetric>();
        // Startujemy silnik aktualizacji danych

    }

    public async Task TestConnection()
    {
        try
        {
            var service = new RiskDataService();
            var data = await service.GetInitialSnapshotAsync();
            // Jeśli tu dojdziesz i data.Count == 100000, to jesteś KRÓLEM!
            System.Diagnostics.Debug.WriteLine($"SUCCESS: Loaded {data.Count} records!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: {ex.Message}");
        }
    }
}