using SendBoxFluid.Services;

var builder = WebApplication.CreateBuilder(args);

// Porta: usa PORT do ambiente (Render) ou 7078 local
var port = Environment.GetEnvironmentVariable("PORT") ?? "7078";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Store em memória — singleton compartilhado entre controllers
builder.Services.AddSingleton<DocumentStore>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

app.Run();
