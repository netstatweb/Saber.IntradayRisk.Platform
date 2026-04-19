using System.Data;
using Microsoft.Data.SqlClient;
using Saber.Risk.Core.Models;
using Saber.Risk.Core.Services;
using Saber.Risk.Core.Utils;

namespace Saber.Risk.Api.Services
{
    /// <summary>
    /// SQL Server implementation of <see cref="IRiskDataService"/>.
    /// Reads connection string from IConfiguration ("SaberRiskConnection") and calls stored procedures / queries.
    /// </summary>
    /// // PL: Implementacja serwisowa korzystająca z MS SQL. Odczytuje connection string z IConfiguration i wykonuje procedury/SELECTy.
    public class SqlRiskDataService : IRiskDataService
    {
        private readonly string _connString;

        /// <summary>
        /// Creates new instance with configuration (expects "ConnectionStrings:SaberRiskConnection").
        /// </summary>
        /// <param name="configuration">IConfiguration from DI.</param>
        public SqlRiskDataService(IConfiguration configuration)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));
            _connString = configuration.GetConnectionString("SaberRiskConnection")
                          ?? throw new InvalidOperationException("Connection string 'SaberRiskConnection' not found in configuration.");
        }

        /// <inheritdoc />
        public async Task<PagedData<RiskMetricDto>> GetPagedAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default)
        {
            var result = new PagedData<RiskMetricDto>();
            var items = new List<RiskMetricDto>();

            using (var conn = new SqlConnection(_connString))
            using (var cmd = new SqlCommand("dbo.GetRiskMetricsPaged", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    // First resultset: page rows
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var dto = new RiskMetricDto
                        {
                            Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                            Ticker = reader["Ticker"]?.ToString() ?? string.Empty,
                            Book = reader["Book"]?.ToString() ?? string.Empty,
                            Currency = reader["Currency"]?.ToString() ?? string.Empty,
                            Delta = reader["Delta"] != DBNull.Value ? Convert.ToDouble(reader["Delta"]) : 0.0,
                            Gamma = reader["Gamma"] != DBNull.Value ? Convert.ToDouble(reader["Gamma"]) : 0.0,
                            Vega = reader["Vega"] != DBNull.Value ? Convert.ToDouble(reader["Vega"]) : 0.0,
                            Exposure = reader["Exposure"] != DBNull.Value ? Convert.ToDouble(reader["Exposure"]) : 0.0,
                        };

                        // optional LastUpdatedUtc column
                        if (reader["LastUpdatedUtc"] != DBNull.Value)
                        {
                            if (DateTime.TryParse(reader["LastUpdatedUtc"]?.ToString(), out var dt))
                                dto.LastUpdatedUtc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        }

                        items.Add(dto);
                    }

                    // Move to second resultset: total count
                    if (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            result.TotalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }

            result.Items = items;
            return result;
        }

        /// <inheritdoc />
        public async Task<RiskMetricDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            const string sql = @"SELECT TOP (1) Id, Ticker, Book, Currency, Delta, Gamma, Vega, Exposure, LastUpdatedUtc
                                 FROM dbo.CurrentRisk
                                 WHERE Id = @Id;";

            using (var conn = new SqlConnection(_connString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var dto = new RiskMetricDto
                        {
                            Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                            Ticker = reader["Ticker"]?.ToString() ?? string.Empty,
                            Book = reader["Book"]?.ToString() ?? string.Empty,
                            Currency = reader["Currency"]?.ToString() ?? string.Empty,
                            Delta = reader["Delta"] != DBNull.Value ? Convert.ToDouble(reader["Delta"]) : 0.0,
                            Gamma = reader["Gamma"] != DBNull.Value ? Convert.ToDouble(reader["Gamma"]) : 0.0,
                            Vega = reader["Vega"] != DBNull.Value ? Convert.ToDouble(reader["Vega"]) : 0.0,
                            Exposure = reader["Exposure"] != DBNull.Value ? Convert.ToDouble(reader["Exposure"]) : 0.0,
                        };

                        if (reader["LastUpdatedUtc"] != DBNull.Value)
                        {
                            if (DateTime.TryParse(reader["LastUpdatedUtc"]?.ToString(), out var dt))
                                dto.LastUpdatedUtc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        }

                        return dto;
                    }
                }
            }

            return null;
        }
    }
}