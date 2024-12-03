using Microsoft.Extensions.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class DatabaseInitialiser : IHostedService
{
    private readonly SqliteConnection _dbConnection;
    private readonly ILogger<DatabaseInitialiser> _logger;

    public DatabaseInitialiser(SqliteConnection dbConnection, ILogger<DatabaseInitialiser> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)  // Marked as async
    {
        _logger.LogInformation("Initializing database...");

        try
        {
            LogDatabaseType();
            LogExistingTables();

            await _dbConnection.OpenAsync();  // Use async open method
            var command = _dbConnection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Results (
                    Id INTEGER PRIMARY KEY,
                    Endpoint TEXT NOT NULL,
                    Response TEXT NOT NULL,
                    Status INT NOT NULL,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Duration REAL NOT NULL  -- Add Duration field
                );
                CREATE TABLE IF NOT EXISTS Endpoints (
                    Id INTEGER PRIMARY KEY,
                    FriendlyName TEXT NOT NULL,
                    Url TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Settings (
                    Id INTEGER PRIMARY KEY,
                    CheckInterval INTEGER NOT NULL,
                    AvgResponseTimeWindow INTEGER NOT NULL  -- Add AvgResponseTimeWindow field
                );";
            await command.ExecuteNonQueryAsync();  // Use async execution

            command.CommandText = "SELECT COUNT(*) FROM Settings";
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());  // Use async scalar execution

            if (count == 0)
            {
                _logger.LogInformation("No settings found. Inserting default settings...");
                command.CommandText = @"INSERT INTO Settings (Id, CheckInterval, AvgResponseTimeWindow)
                                         VALUES (1, 30, 10);";
                await command.ExecuteNonQueryAsync();  // Use async execution
                _logger.LogInformation("Default settings inserted.");
            }

            _logger.LogInformation("Database initialized successfully.");

            LogExistingTables();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database.");
            throw;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    private void LogDatabaseType()
    {
        _logger.LogInformation("Database type: {Type}", _dbConnection.DataSource);
    }

    private void LogExistingTables()
    {
        _logger.LogInformation("Logging existing tables...");

        _dbConnection.Open();
        var command = _dbConnection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                _logger.LogInformation("Table found: {TableName}", reader["name"]);
            }
        }

        _dbConnection.Close();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
