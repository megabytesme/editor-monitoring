using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class EndpointTesterController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly SqliteConnection _dbConnection;
    private readonly ILogger<EndpointTesterController> _logger;

    public EndpointTesterController(HttpClient httpClient, SqliteConnection dbConnection, ILogger<EndpointTesterController> logger)
    {
        _httpClient = httpClient;
        _dbConnection = dbConnection;
        _logger = logger;
    }

    [HttpPost]
    [Route("test-endpoint")]
    public async Task<IActionResult> TestEndpoint([FromBody] TestRequest request)
    {
        _logger.LogInformation("Received request to test endpoint: {Endpoint}", request.Endpoint);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var response = await _httpClient.GetAsync(request.Endpoint);
        var content = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;
        _logger.LogInformation("Received response with status code: {StatusCode}", statusCode);

        stopwatch.Stop();
        var duration = stopwatch.Elapsed.TotalMilliseconds;

        await LogResultAsync(request.Endpoint, content, statusCode, duration);

        return Ok(new { message = statusCode == 200 ? "Success" : "Failure", duration });
    }

    [HttpGet]
    [Route("results")]
    public async Task<IActionResult> GetResults()
    {
        _logger.LogInformation("Fetching results from database...");
        try
        {
            await _dbConnection.OpenAsync();
            var command = _dbConnection.CreateCommand();
            command.CommandText = "SELECT * FROM Results";

            var results = new List<Result>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    results.Add(new Result
                    {
                        Id = reader.GetInt32(0),
                        Endpoint = reader.GetString(1),
                        Response = reader.GetString(2),
                        Status = reader.GetInt32(3),
                        Timestamp = reader.GetDateTime(4),
                        Duration = reader.GetDouble(5)
                    });
                }
            }
            _logger.LogInformation("Results fetched successfully.");
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching results from database.");
            throw;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    private async Task LogResultAsync(string endpoint, string response, int statusCode, double duration)
    {
        _logger.LogInformation("Logging result to database. Endpoint: {Endpoint}, Response: {Response}, Status: {StatusCode}, Duration: {Duration}", endpoint, response, statusCode, duration);
        try
        {
            await _dbConnection.OpenAsync();
            var command = _dbConnection.CreateCommand();
            command.CommandText = @"INSERT INTO Results (Endpoint, Response, Status, Timestamp, Duration) VALUES (@endpoint, @response, @statusCode, @timestamp, @duration)";
            command.Parameters.AddWithValue("@endpoint", endpoint);
            command.Parameters.AddWithValue("@response", response);
            command.Parameters.AddWithValue("@statusCode", statusCode);
            command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
            command.Parameters.AddWithValue("@duration", duration);
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Result logged successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging result to database.");
            throw;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }
}

public class TestRequest
{
    public string Endpoint { get; set; }
}

public class Result
{
    public int Id { get; set; }
    public string Endpoint { get; set; }
    public string Response { get; set; }
    public int Status { get; set; }
    public DateTime Timestamp { get; set; }
    public double Duration { get; set; }
}
