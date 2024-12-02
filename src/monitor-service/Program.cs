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
    options.AddPolicy("AllowLocalhost3000",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000")
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "monitoring.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)); // Ensure the directory exists
string connectionString = $"Data Source={dbPath}";
builder.Services.AddSingleton(new SqliteConnection(connectionString));
builder.Services.AddHostedService<DatabaseInitialiser>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.UseCors("AllowLocalhost3000");

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers().RequireCors("AllowLocalhost3000");
});

app.Run();
