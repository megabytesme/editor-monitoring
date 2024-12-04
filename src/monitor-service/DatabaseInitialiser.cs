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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing database...");

        try
        {
            LogDatabaseType();
            await _dbConnection.OpenAsync(cancellationToken);

            await CreateTableAsync(
                @"CREATE TABLE IF NOT EXISTS Settings (
                Id INTEGER PRIMARY KEY,
                CheckInterval INTEGER NOT NULL,
                AvgResponseTimeWindow INTEGER NOT NULL,
                EmailAddress TEXT NOT NULL,
                MaxThresholdDuration INTEGER NOT NULL
            );", cancellationToken);

            await CreateTableAsync(
                @"CREATE TABLE IF NOT EXISTS Endpoints (
                Id INTEGER PRIMARY KEY,
                FriendlyName TEXT NOT NULL,
                Url TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );", cancellationToken);

            await CreateTableAsync(
                @"CREATE TABLE IF NOT EXISTS Results (
                Id INTEGER PRIMARY KEY,
                EndpointId INTEGER NOT NULL,
                Status TEXT NOT NULL,
                ResponseTime INTEGER NOT NULL,
                Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (EndpointId) REFERENCES Endpoints(Id)
            );", cancellationToken);

            await InsertDefaultSettingsAsync(cancellationToken);

            LogExistingTables();
            _logger.LogInformation("Database initialized successfully.");
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

    private async Task CreateTableAsync(string createTableSql, CancellationToken cancellationToken)
    {
        var command = _dbConnection.CreateCommand();
        command.CommandText = createTableSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task InsertDefaultSettingsAsync(CancellationToken cancellationToken)
    {
        var checkSettingsCommand = _dbConnection.CreateCommand();
        checkSettingsCommand.CommandText = "SELECT COUNT(*) FROM Settings";
        var count = Convert.ToInt32(await checkSettingsCommand.ExecuteScalarAsync(cancellationToken));

        if (count == 0)
        {
            _logger.LogInformation("No settings found. Inserting default settings...");
            var insertDefaultsCommand = _dbConnection.CreateCommand();
            insertDefaultsCommand.CommandText = @"
            INSERT INTO Settings (Id, CheckInterval, AvgResponseTimeWindow, EmailAddress, MaxThresholdDuration)
            VALUES (1, 30, 10, 'default@example.com', 1000);
        ";
            await insertDefaultsCommand.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Default settings inserted.");
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
