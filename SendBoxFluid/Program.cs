using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Porta: usa PORT do ambiente (Render) ou 7078 local
var port = Environment.GetEnvironmentVariable("PORT") ?? "7078";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
