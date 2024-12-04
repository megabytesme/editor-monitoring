using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public class EndpointCheckerService : BackgroundService
{
    private readonly SqliteConnection _dbConnection;
    private readonly HttpClient _httpClient;
    private readonly ILogger<EndpointCheckerService> _logger;
    private int _checkInterval;

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
                _checkInterval = await GetCheckIntervalAsync();
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

    private async Task<int> GetCheckIntervalAsync()
    {
        _logger.LogInformation("Fetching CheckInterval from database...");
        await _dbConnection.OpenAsync();
        try
        {
            var command = _dbConnection.CreateCommand();
            command.CommandText = "SELECT CheckInterval FROM Settings LIMIT 1";
            return Convert.ToInt32(await command.ExecuteScalarAsync());
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

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:80/api/EndpointTester/test-endpoint")
        {
            Content = new StringContent($"{{ \"endpoint\": \"{endpoint}\" }}", System.Text.Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, token);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to test endpoint {Endpoint}: {StatusCode}", endpoint, response.StatusCode);
        }
        else
        {
            _logger.LogInformation("Successfully tested endpoint: {Endpoint}", endpoint);
        }
    }
}
