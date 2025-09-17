using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace FMSoftlab.HealthCheck
{
    public interface IHealthcheckDatabaseOptions
    {
        string ConnectionString { get; set; }
    }
    public class HealthcheckDatabaseOptions : IHealthcheckDatabaseOptions
    {
        public string ConnectionString { get; set; }

        public HealthcheckDatabaseOptions(string connectionString)
        {
            ConnectionString=connectionString;
        }

        public HealthcheckDatabaseOptions()
        {
            ConnectionString=string.Empty;
        }
    }
    public class StdDatabaseHealthCheck : IHealthCheck
    {
        private readonly IHealthcheckDatabaseOptions _settings;
        private readonly ILogger<StdDatabaseHealthCheck> _log;
        public StdDatabaseHealthCheck(IHealthcheckDatabaseOptions settings, ILogger<StdDatabaseHealthCheck> log)
        {
            _settings=settings;
            _log=log;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_settings.ConnectionString))
                {
                    con.Open();
                    DateTime? dbdate = await con.ExecuteScalarAsync<DateTime>("select getdate() as currentdate");
                    string message = $"db connection ok, db date:{dbdate}";
                    _log?.LogInformation(message);
                    return HealthCheckResult.Healthy(message);
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                return HealthCheckResult.Unhealthy($"unable to connect to db:{ex.Message}");
            }
        }
    }

    public static class StdDatabaseHealthCheckExtensions
    {
        public static void AddStdHealtChecks(this IServiceCollection services, string connectionString)
        {

            services.AddTransient<IHealthcheckDatabaseOptions, HealthcheckDatabaseOptions>(sp =>
            {
                return new HealthcheckDatabaseOptions(connectionString);
            });
            services.AddTransient<StdDatabaseHealthCheck>();
            services
                .AddHealthChecks()
                .AddCheck<StdDatabaseHealthCheck>("Database");
        }
    }
}