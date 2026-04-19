using System.Configuration;
using System.Data;
using System.Windows;

namespace Saber.Risk.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // TO JEST KLUCZOWE:
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }

}
