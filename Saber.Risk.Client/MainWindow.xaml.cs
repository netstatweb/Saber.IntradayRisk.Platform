using Saber.Risk.Client.ViewModels;
using System.Windows;

namespace Saber.Risk.Client
{
    /// <summary>
    /// Logika interakcji dla MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(string token) // Przyjmujemy token z okna logowania
        {
            InitializeComponent();

            // 1. Najpierw tworzymy instancję ViewModelu
            var vm = new RiskDashboardViewModel();

            // 2. Przypisujemy ją do DataContext (żeby bindowanie w XAML działało)
            this.DataContext = vm;

            // 3. Przekazujemy token do ViewModelu, aby odblokować dostęp do API
            vm.SetToken(token);
        }
    }
}