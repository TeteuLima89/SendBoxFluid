using SendBoxFluid.Domain.Interfaces;
using SendBoxFluid.Domain.Services;
using SendBoxFluid.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "7078";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// DI — Domain
builder.Services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
builder.Services.AddSingleton<DocumentGeneratorService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
