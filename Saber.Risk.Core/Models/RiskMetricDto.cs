using System;

namespace Saber.Risk.Core.Models
{
    /// <summary>
    /// Data transfer object representing a single risk metric row shared across services and clients.
    /// </summary>
    /// // PL: DTO reprezentujący pojedynczy wiersz metryk ryzyka, używany pomiędzy serwisami i klientami.
    public class RiskMetricDto
    {
        /// <summary>Unique identifier for the metric row.</summary>
        /// // PL: Unikalny identyfikator wiersza.
        public int Id { get; set; }

        /// <summary>Ticker symbol.</summary>
        /// // PL: Symbol instrumentu.
        public string Ticker { get; set; } = string.Empty;

        /// <summary>Book name.</summary>
        /// // PL: Księga (book) przypisana do pozycji.
        public string Book { get; set; } = string.Empty;

        /// <summary>Currency code.</summary>
        /// // PL: Kod waluty.
        public string Currency { get; set; } = string.Empty;

        /// <summary>Delta value.</summary>
        /// // PL: Wartość delty.
        public double Delta { get; set; }

        /// <summary>Gamma value.</summary>
        /// // PL: Wartość gammy.
        public double Gamma { get; set; }

        /// <summary>Vega value.</summary>
        /// // PL: Wartość vegi.
        public double Vega { get; set; }

        /// <summary>Monetary exposure.</summary>
        /// // PL: Ekspozycja w walucie.
        public double Exposure { get; set; }

        /// <summary>UTC timestamp when this row was last updated.</summary>
        /// // PL: Znacznik czasu (UTC) ostatniej aktualizacji.
        public DateTime LastUpdatedUtc { get; set; }
    }
}