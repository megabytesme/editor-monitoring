using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.Sqlite;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddLogging();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "monitoring.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
string connectionString = $"Data Source={dbPath}";
builder.Services.AddSingleton(new SqliteConnection(connectionString));

builder.Services.AddHostedService<DatabaseInitialiser>();
builder.Services.AddHostedService<EndpointCheckerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowAllOrigins");

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
