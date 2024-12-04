using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonitorService.Tests
{
    [TestFixture]
    public class ConfigControllerTests
    {
        private Mock<ILogger<ConfigController>> _loggerMock;
        private SqliteConnection _dbConnection;
        private ConfigController _controller;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<ConfigController>>();
            _dbConnection = new SqliteConnection("Data Source=:memory:");
            _dbConnection.Open();

            var createEndpointsTable = @"
                CREATE TABLE Endpoints (Id INTEGER PRIMARY KEY, FriendlyName TEXT, Url TEXT);
                CREATE TABLE Settings (Id INTEGER PRIMARY KEY, CheckInterval INTEGER, AvgResponseTimeWindow INTEGER, EmailAddress TEXT, MaxThresholdDuration INTEGER);";
            var command = _dbConnection.CreateCommand();
            command.CommandText = createEndpointsTable;
            command.ExecuteNonQuery();

            _controller = new ConfigController(_dbConnection, _loggerMock.Object);
        }

        [Test]
        public async Task GetEndpoints_ReturnsEndpoints()
        {
            // Arrange
            var insertEndpoint = "INSERT INTO Endpoints (FriendlyName, Url) VALUES ('Test Endpoint', 'http://example.com')";
            var command = _dbConnection.CreateCommand();
            command.CommandText = insertEndpoint;
            command.ExecuteNonQuery();

            // Act
            var result = await _controller.GetEndpoints();

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = (OkObjectResult)result;
            var endpoints = (List<EndpointConfig>)okResult.Value;
            Assert.AreEqual(1, endpoints.Count);
            Assert.AreEqual("Test Endpoint", endpoints[0].FriendlyName);
            Assert.AreEqual("http://example.com", endpoints[0].Url);
        }

        [Test]
        public async Task AddEndpoint_AddsNewEndpoint()
        {
            // Arrange
            var newEndpoint = new EndpointConfig { FriendlyName = "New Endpoint", Url = "http://newendpoint.com" };

            // Act
            var result = await _controller.AddEndpoint(newEndpoint);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = (OkObjectResult)result;
            var addedEndpoint = (EndpointConfig)okResult.Value;
            Assert.AreEqual("New Endpoint", addedEndpoint.FriendlyName);
            Assert.AreEqual("http://newendpoint.com", addedEndpoint.Url);
        }

        [Test]
        public async Task GetSettings_ReturnsSettings()
        {
            // Arrange
            var insertSettings = "INSERT INTO Settings (CheckInterval, AvgResponseTimeWindow, EmailAddress, MaxThresholdDuration) VALUES (30, 10, 'test@example.com', 1000)";
            var command = _dbConnection.CreateCommand();
            command.CommandText = insertSettings;
            command.ExecuteNonQuery();

            // Act
            var result = await _controller.GetSettings();

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = (OkObjectResult)result;
            var settings = (SettingsConfig)okResult.Value;
            Assert.AreEqual(30, settings.CheckInterval);
            Assert.AreEqual("test@example.com", settings.EmailAddress);
        }

        [Test]
        public async Task UpdateSettings_UpdatesSettings()
        {
            // Arrange
            var newSettings = new SettingsConfig
            {
                Id = 1,
                CheckInterval = 60,
                AvgResponseTimeWindow = 15,
                EmailAddress = "updated@example.com",
                MaxThresholdDuration = 1500
            };

            // Act
            var result = await _controller.UpdateSettings(newSettings);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = (OkObjectResult)result;
            var updatedSettings = (SettingsConfig)okResult.Value;
            Assert.AreEqual(60, updatedSettings.CheckInterval);
            Assert.AreEqual("updated@example.com", updatedSettings.EmailAddress);
        }
    }
}
