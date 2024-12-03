using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly SqliteConnection _dbConnection;
    private readonly ILogger<StatusController> _logger;

    public StatusController(SqliteConnection dbConnection, ILogger<StatusController> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    [HttpGet("results")]
    public async Task<IActionResult> GetResults()
    {
        _logger.LogInformation("Fetching status results from database...");
        try
        {
            await _dbConnection.OpenAsync();
            var command = _dbConnection.CreateCommand();
            command.CommandText = "SELECT * FROM Results";

            var results = new List<StatusResult>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    results.Add(new StatusResult
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
            _logger.LogInformation("Status results fetched successfully.");
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching status results from database.");
            throw;
        }
        finally
        {
            if (_dbConnection.State == System.Data.ConnectionState.Open)
            {
                await _dbConnection.CloseAsync();
            }
        }
    }
}

public class StatusResult
{
    public int Id { get; set; }
    public string Endpoint { get; set; }
    public string Response { get; set; }
    public int Status { get; set; }
    public DateTime Timestamp { get; set; }
    public double Duration { get; set; }
}
