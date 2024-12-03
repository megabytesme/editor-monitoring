using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly SqliteConnection _dbConnection;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(SqliteConnection dbConnection, ILogger<ConfigController> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    [HttpGet("endpoints")]
    public async Task<IActionResult> GetEndpoints()
    {
        _logger.LogInformation("Fetching endpoints from database...");
        try
        {
            await _dbConnection.OpenAsync();
            var command = _dbConnection.CreateCommand();
            command.CommandText = "SELECT * FROM Endpoints";

            var endpoints = new List<EndpointConfig>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    endpoints.Add(new EndpointConfig
                    {
                        Id = reader.GetInt32(0),
                        FriendlyName = reader.GetString(1),
                        Url = reader.GetString(2)
                    });
                }
            }
            _logger.LogInformation("Endpoints fetched successfully.");
            return Ok(endpoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching endpoints from database.");
            throw;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    [HttpPost("endpoints")]
    public async Task<IActionResult> AddEndpoint([FromBody] EndpointConfig endpoint)
    {
        _logger.LogInformation("Adding endpoint to database...");
        try
        {
            await _dbConnection.OpenAsync();
            var command = _dbConnection.CreateCommand();
            command.CommandText = @"INSERT INTO Endpoints (FriendlyName, Url) VALUES (@friendlyName, @url);
                                    SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@friendlyName", endpoint.FriendlyName);
            command.Parameters.AddWithValue("@url", endpoint.Url);
            endpoint.Id = (int)(long)await command.ExecuteScalarAsync();

            _logger.LogInformation("Endpoint added successfully.");
            return Ok(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding endpoint to database.");
            throw;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    [HttpDelete("endpoints/{id}")]
    public async Task<IActionResult> DeleteEndpoint(int id)
    {
        _logger.LogInformation("Deleting endpoint from database...");
        try
        {
            await _dbConnection.OpenAsync();
            var command = _dbConnection.CreateCommand();
            command.CommandText = "DELETE FROM Endpoints WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Endpoint deleted successfully.");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting endpoint from database.");
            throw;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        _logger.LogInformation("Fetching settings from database...");
        try
        {
            await _dbConnection.OpenAsync();
            var command = _dbConnection.CreateCommand();
            command.CommandText = "SELECT * FROM Settings LIMIT 1";

            SettingsConfig settings = null;
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    settings = new SettingsConfig
                    {
                        Id = reader.GetInt32(0),
                        CheckInterval = reader.GetInt32(1),
                        AvgResponseTimeWindow = reader.GetInt32(2)
                    };
                }
            }

            _logger.LogInformation("Settings fetched successfully.");
            return Ok(settings ?? new SettingsConfig { Id = 1, CheckInterval = 30, AvgResponseTimeWindow = 10 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching settings from database.");
            throw;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] SettingsConfig settings)
    {
        _logger.LogInformation("Updating settings in database...");
        try
        {
            await _dbConnection.OpenAsync();
            var command = _dbConnection.CreateCommand();
            command.CommandText = "REPLACE INTO Settings (Id, CheckInterval, AvgResponseTimeWindow) VALUES (@id, @checkInterval, @avgResponseTimeWindow)";
            command.Parameters.AddWithValue("@id", settings.Id);
            command.Parameters.AddWithValue("@checkInterval", settings.CheckInterval);
            command.Parameters.AddWithValue("@avgResponseTimeWindow", settings.AvgResponseTimeWindow);
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Settings updated successfully.");
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings in database.");
            throw;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }
}

public class EndpointConfig
{
    public int Id { get; set; }
    public string FriendlyName { get; set; }
    public string Url { get; set; }
}

public class SettingsConfig
{
    public int Id { get; set; }
    public int CheckInterval { get; set; }
    public int AvgResponseTimeWindow { get; set; }
}
