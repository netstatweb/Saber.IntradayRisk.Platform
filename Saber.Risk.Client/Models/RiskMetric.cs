using Saber.Risk.Client.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Saber.Risk.Client.Models
{
    public class RiskMetric : ViewModelBase
    {
        public string Ticker { get; set; }
        public string Book { get; set; }

        private double _delta;
        public double Delta
        {
            get => _delta;
            set { _delta = value; OnPropertyChanged(); }
        }

        private double _gamma;
        public double Gamma
        {
            get => _gamma;
            set { _gamma = value; OnPropertyChanged(); }
        }
    }
}
