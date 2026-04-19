using Saber.Risk.Client.Core;
using System.Threading.Tasks;
using System.Windows;

namespace Saber.Risk.Client.Models
{
    public enum PriceAction { None, Increase, Decrease }

    public class RiskMetric : ViewModelBase
    {
        public string Ticker { get; set; }
        public string Book { get; set; }
        public string Currency { get; set; }

        // Stan wizualny dla Delty
        private PriceAction _deltaAction;
        public PriceAction DeltaAction
        {
            get => _deltaAction;
            set { _deltaAction = value; OnPropertyChanged(nameof(DeltaAction)); }
        }

        private double _deltaDiff;
        public double DeltaDiff
        {
            get => _deltaDiff;
            set { _deltaDiff = value; OnPropertyChanged(nameof(DeltaDiff)); }
        }

        private double _delta;
        public double Delta
        {
            get => _delta;
            set
            {
                // Jeśli wartość się nie zmieniła, nic nie rób
                if (System.Math.Abs(_delta - value) < 0.000001) return;

                // 1. NAJPIERW OBLICZAMY KIERUNEK I RÓŻNICĘ
                DeltaDiff = value - _delta;
                DeltaAction = value > _delta ? PriceAction.Increase : PriceAction.Decrease;

                // 2. NIE PRZYPISUJEMY JESZCZE 'value' DO '_delta'
                // Zamiast tego odpalamy opóźnienie (np. 1500ms), 
                // po którym dopiero UI zobaczy nową liczbę
                UpdateValueWithDelay(value);
            }
        }

        private async void UpdateValueWithDelay(double newValue)
        {
            // Czekamy 1.5 sekundy, podczas gdy komórka już miga kolorem
            await Task.Delay(1800);

            // 3. DOPIERO TERAZ AKTUALIZUJEMY LICZBĘ
            _delta = newValue;
            OnPropertyChanged(nameof(Delta));

            // Opcjonalnie: wygaszamy kolor chwilę po zmianie liczby
            await Task.Delay(1800);
            DeltaAction = PriceAction.None;
        }


        private async void ResetHighlight(string actionProperty, int delayMs)
        {
            await Task.Delay(delayMs);
            Application.Current.Dispatcher.Invoke(() => {
                if (actionProperty == nameof(DeltaAction)) DeltaAction = PriceAction.None;
                if (actionProperty == nameof(ExposureAction)) ExposureAction = PriceAction.None;
            });
        }

        // Powtórz tę logikę dla Exposure (najbardziej efektowne)
        private PriceAction _exposureAction;
        public PriceAction ExposureAction
        {
            get => _exposureAction;
            set { _exposureAction = value; OnPropertyChanged(nameof(ExposureAction)); }
        }

        private double _exposure;
        public double Exposure
        {
            get => _exposure;
            set
            {
                ExposureAction = value > _exposure ? PriceAction.Increase : PriceAction.Decrease;
                _exposure = value;
                OnPropertyChanged(nameof(Exposure));
                ResetHighlight(nameof(ExposureAction));
            }
        }

        // Metoda resetująca stan po mignięciu
        private async void ResetHighlight(string actionProperty)
        {
            await Task.Delay(600); // Czas trwania podświetlenia
            if (actionProperty == nameof(DeltaAction)) DeltaAction = PriceAction.None;
            if (actionProperty == nameof(ExposureAction)) ExposureAction = PriceAction.None;
        }

        public double Gamma { get; set; } // Możesz dodać flashing też tutaj
        public double Vega { get; set; }
    }
}