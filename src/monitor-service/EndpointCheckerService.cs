using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Mail;

public class EndpointCheckerService : BackgroundService
{
    private readonly SqliteConnection _dbConnection;
    private readonly HttpClient _httpClient;
    private readonly ILogger<EndpointCheckerService> _logger;
    private int _checkInterval;
    private int _maxThresholdDuration;
    private string _emailAddress;

    public EndpointCheckerService(
        SqliteConnection dbConnection,
        HttpClient httpClient,
        ILogger<EndpointCheckerService> logger)
    {
        _dbConnection = dbConnection;
        _httpClient = httpClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting EndpointCheckerService...");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await LoadSettingsAsync();
                var endpoints = await GetEndpointsAsync();

                _logger.LogInformation("Checking {Count} endpoints...", endpoints.Count);

                foreach (var endpoint in endpoints)
                {
                    await TestEndpointAsync(endpoint, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking endpoints.");
            }

            _logger.LogInformation("Waiting for {CheckInterval} seconds...", _checkInterval);
            await Task.Delay(_checkInterval * 1000, stoppingToken);
        }
    }

    private async Task LoadSettingsAsync()
    {
        _logger.LogInformation("Fetching settings from database...");
        await _dbConnection.OpenAsync();
        try
        {
            var command = _dbConnection.CreateCommand();
            command.CommandText = "SELECT CheckInterval, MaxThresholdDuration, EmailAddress FROM Settings LIMIT 1";
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    _checkInterval = reader.GetInt32(reader.GetOrdinal("CheckInterval"));
                    _maxThresholdDuration = reader.GetInt32(reader.GetOrdinal("MaxThresholdDuration"));
                    _emailAddress = reader.GetString(reader.GetOrdinal("EmailAddress"));
                }
                else
                {
                    throw new InvalidOperationException("Settings not found in the database.");
                }
            }
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    private async Task<List<string>> GetEndpointsAsync()
    {
        _logger.LogInformation("Fetching endpoints from database...");
        var endpoints = new List<string>();

        await _dbConnection.OpenAsync();
        try
        {
            var command = _dbConnection.CreateCommand();
            command.CommandText = "SELECT Url FROM Endpoints";
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    endpoints.Add(reader.GetString(0));
                }
            }
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }

        return endpoints;
    }

    private async Task TestEndpointAsync(string endpoint, CancellationToken token)
    {
        _logger.LogInformation("Testing endpoint: {Endpoint}", endpoint);

        try
        {
            var start = DateTime.UtcNow;
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:80/api/EndpointTester/test-endpoint")
            {
                Content = new StringContent($"{{ \"endpoint\": \"{endpoint}\" }}", System.Text.Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request, token);
            var duration = (DateTime.UtcNow - start).TotalMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Endpoint {Endpoint} returned error status: {StatusCode}", endpoint, response.StatusCode);
                Task.Run(() => SendAlertEmailAsync(endpoint, $"Error status code: {response.StatusCode}"));
            }

            if (duration > _maxThresholdDuration)
            {
                _logger.LogWarning("Endpoint {Endpoint} response time {Duration} ms exceeds the threshold of {Threshold} ms", endpoint, duration, _maxThresholdDuration);
                Task.Run(() => SendAlertEmailAsync(endpoint, $"Response time exceeded: {duration} ms"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while testing endpoint: {Endpoint}", endpoint);
            Task.Run(() => SendAlertEmailAsync(endpoint, $"Exception occurred: {ex.Message}"));
        }
    }

    private async Task SendAlertEmailAsync(string endpoint, string issue)
    {
        try
        {
            _logger.LogInformation("Sending alert email for endpoint {Endpoint}...", endpoint);
            var mailMessage = new MailMessage("monitor@yourapp.com", _emailAddress)
            {
                Subject = "Endpoint Alert",
                Body = $"An issue was detected with endpoint {endpoint}:\n\n{issue}"
            };

            using var smtpClient = new SmtpClient(Environment.GetEnvironmentVariable("SMTP_HOST") ?? "localhost")
            {
                Port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "25"),
                Credentials = new System.Net.NetworkCredential(
                    Environment.GetEnvironmentVariable("SMTP_USER") ?? "",
                    Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? ""
                ),
                EnableSsl = bool.Parse(Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL") ?? "false"),
            };

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Alert email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert email for endpoint {Endpoint}", endpoint);
        }
    }
}
