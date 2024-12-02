using Microsoft.AspNetCore.Mvc;
using System;
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

        var response = await _httpClient.GetAsync(request.Endpoint);
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Received response: {Response}", content);

        var isValid = ValidateResponse(content);
        _logger.LogInformation("Validation result: {IsValid}", isValid);

        await LogResultAsync(request.Endpoint, content, isValid);

        return Ok(new { message = isValid ? "Success" : "Failure" });
    }

    private bool ValidateResponse(string content)
    {
        // TODO
        return true;
    }

    private async Task LogResultAsync(string endpoint, string response, bool isValid)
    {
        _logger.LogInformation("Logging result to database. Endpoint: {Endpoint}, Response: {Response}, IsValid: {IsValid}", endpoint, response, isValid);
        try
        {
            await _dbConnection.OpenAsync();
            var command = _dbConnection.CreateCommand();
            command.CommandText = @"INSERT INTO Results (Endpoint, Response, IsValid) VALUES (@endpoint, @response, @isValid)";
            command.Parameters.AddWithValue("@endpoint", endpoint);
            command.Parameters.AddWithValue("@response", response);
            command.Parameters.AddWithValue("@isValid", isValid);
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
