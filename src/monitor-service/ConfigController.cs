using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        return await ExecuteDatabaseCommand(async command =>
        {
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
            return Ok(endpoints);
        });
    }

    [HttpPost("endpoints")]
    public async Task<IActionResult> AddEndpoint([FromBody] EndpointConfig endpoint)
    {
        return await ExecuteDatabaseCommand(async command =>
        {
            command.CommandText = @"INSERT INTO Endpoints (FriendlyName, Url) VALUES (@friendlyName, @url);
                                    SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@friendlyName", endpoint.FriendlyName);
            command.Parameters.AddWithValue("@url", endpoint.Url);
            endpoint.Id = (int)(long)await command.ExecuteScalarAsync();
            return Ok(endpoint);
        });
    }

    [HttpDelete("endpoints/{id}")]
    public async Task<IActionResult> DeleteEndpoint(int id)
    {
        return await ExecuteDatabaseCommand(async command =>
        {
            command.CommandText = "DELETE FROM Endpoints WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);
            await command.ExecuteNonQueryAsync();
            return NoContent();
        });
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        return await ExecuteDatabaseCommand(async command =>
        {
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
            return Ok(settings ?? new SettingsConfig { Id = 1, CheckInterval = 30, AvgResponseTimeWindow = 10 });
        });
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] SettingsConfig settings)
    {
        return await ExecuteDatabaseCommand(async command =>
        {
            command.CommandText = "REPLACE INTO Settings (Id, CheckInterval, AvgResponseTimeWindow) VALUES (@id, @checkInterval, @avgResponseTimeWindow)";
            command.Parameters.AddWithValue("@id", settings.Id);
            command.Parameters.AddWithValue("@checkInterval", settings.CheckInterval);
            command.Parameters.AddWithValue("@avgResponseTimeWindow", settings.AvgResponseTimeWindow);
            await command.ExecuteNonQueryAsync();
            return Ok(settings);
        });
    }

    private async Task<IActionResult> ExecuteDatabaseCommand(Func<SqliteCommand, Task<IActionResult>> action)
    {
        try
        {
            await _dbConnection.OpenAsync();
            var command = _dbConnection.CreateCommand();
            return await action(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database operation failed.");
            return StatusCode(500, "Internal server error");
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
