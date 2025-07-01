using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Services;
using Mind_Mend.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mind_Mend.Services
{
    public interface IGoogleHealthConnectService
    {
        Task<HealthData> GetLatestHealthDataAsync(string userId);
        Task<bool> SyncHealthDataAsync(string userId);
    }

    public class GoogleHealthConnectService : IGoogleHealthConnectService
    {
        private readonly IConfiguration _configuration;
        private readonly FitnessService _fitnessService;
        private readonly ILogger<GoogleHealthConnectService> _logger;

        public GoogleHealthConnectService(
            IConfiguration configuration,
            ILogger<GoogleHealthConnectService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _fitnessService = InitializeFitnessService();
        }

        private FitnessService InitializeFitnessService()
        {
            var credential = GoogleCredential
                .FromFile(_configuration["GoogleHealthConnect:CredentialsPath"])
                .CreateScoped(FitnessService.Scope.FitnessActivityRead);

            return new FitnessService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Mind-Mend"
            });
        }

        public async Task<HealthData> GetLatestHealthDataAsync(string userId)
        {
            try
            {
                // Get the current time in milliseconds since epoch
                var endTimeMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var startTimeMillis = endTimeMillis - (24 * 60 * 60 * 1000); // Last 24 hours

                // Request data for different data types
                var heartRateRequest = _fitnessService.Users.DataSources.Datasets.Get(
                    "me",
                    "derived:com.google.heart_rate.bpm:com.google.android.gms:merge_heart_rate_bpm",
                    $"{startTimeMillis}-{endTimeMillis}");

                var stepsRequest = _fitnessService.Users.DataSources.Datasets.Get(
                    "me",
                    "derived:com.google.step_count.delta:com.google.android.gms:estimated_steps",
                    $"{startTimeMillis}-{endTimeMillis}");

                // Execute requests in parallel
                var heartRateTask = heartRateRequest.ExecuteAsync();
                var stepsTask = stepsRequest.ExecuteAsync();

                await Task.WhenAll(heartRateTask, stepsTask);

                // Process the results and create HealthData object
                var healthData = new HealthData
                {
                    UserId = userId,
                    Timestamp = DateTime.UtcNow,
                    Source = "CMF_WATCH_PRO_2"
                };

                // Process heart rate data
                var heartRateData = await heartRateTask;
                if (heartRateData.Point?.Any() == true)
                {
                    healthData.HeartRate = heartRateData.Point.Last().Value[0].FpVal ?? 0;
                }

                // Process steps data
                var stepsData = await stepsTask;
                if (stepsData.Point?.Any() == true)
                {
                    healthData.Steps = (int)(stepsData.Point.Last().Value[0].IntVal ?? 0);
                }

                return healthData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch health data from Google Health Connect for user {UserId}", userId);
                throw new Exception($"Failed to fetch health data from Google Health Connect: {ex.Message}", ex);
            }
        }

        public async Task<bool> SyncHealthDataAsync(string userId)
        {
            try
            {
                var healthData = await GetLatestHealthDataAsync(userId);
                // Here you would typically save the health data to your database
                // await _healthDataRepository.SaveAsync(healthData);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync health data for user {UserId}", userId);
                return false;
            }
        }
    }
} 