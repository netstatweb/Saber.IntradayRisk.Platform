using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

namespace Saber.Risk.Client
{
    public partial class LoginWindow : Window
    {
        private readonly HttpClient _http = new HttpClient { BaseAddress = new Uri("https://localhost:7240/") };

        public LoginWindow() => InitializeComponent();

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var request = new { Username = UsernameBox.Text, Password = PasswordBox.Password };
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/login", request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                    var main = new MainWindow(result.Token);
                    main.Show();
                    this.Close();
                }
                else { ErrorLabel.Visibility = Visibility.Visible; }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        public class LoginResult { public string Token { get; set; } }
    }
}