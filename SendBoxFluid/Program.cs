using Microsoft.EntityFrameworkCore;
using SendBoxFluid.Aplicacao.Interfaces;
using SendBoxFluid.Aplicacao.Servicos;
using SendBoxFluid.Dominio.Interfaces.Repositorios;
using SendBoxFluid.Dominio.Servicos;
using SendBoxFluid.Infraestrutura.ClientesExternos;
using SendBoxFluid.Infraestrutura.Middlewares;
using SendBoxFluid.Infraestrutura.Persistencia;
using SendBoxFluid.Infraestrutura.Repositorios;
using SendBoxFluid.Infraestrutura.ServicosFundo;

var construtor = WebApplication.CreateBuilder(args);

var porta = Environment.GetEnvironmentVariable("PORT") ?? "7078";
construtor.WebHost.UseUrls($"http://0.0.0.0:{porta}");

construtor.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(opcoes =>
{
    opcoes.ViewLocationFormats.Clear();
    opcoes.ViewLocationFormats.Add("/0 - Apresentacao/Views/{1}/{0}.cshtml");
    opcoes.ViewLocationFormats.Add("/0 - Apresentacao/Views/Shared/{0}.cshtml");
});

// ============== PERSISTENCIA ==============
// Se CONNECTION_STRING_POSTGRES estiver definida, usa Postgres.
// Caso contrario, fallback pra em memoria.
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING_POSTGRES")
                    ?? construtor.Configuration.GetConnectionString("Postgres");

if (!string.IsNullOrEmpty(connectionString))
{
    construtor.Services.AddDbContext<SendBoxDbContext>(opcoes =>
        opcoes.UseNpgsql(NormalizarConnectionString(connectionString)));

    // Repositorios persistidos (escopo singleton, mas DbContext via scope factory)
    construtor.Services.AddSingleton<IRepositorioDocumento, RepositorioDocumentoPostgres>();
    construtor.Services.AddSingleton<IRepositorioSessao, RepositorioSessaoPostgres>();
    construtor.Services.AddSingleton<IRepositorioConfiguracaoNarwal, RepositorioConfiguracaoNarwalPostgres>();

    Console.WriteLine("[SendBox] Persistencia: PostgreSQL");
}
else
{
    construtor.Services.AddSingleton<IRepositorioDocumento, RepositorioDocumentoEmMemoria>();
    construtor.Services.AddSingleton<IRepositorioSessao, RepositorioSessaoEmMemoria>();
    construtor.Services.AddSingleton<IRepositorioConfiguracaoNarwal, RepositorioConfiguracaoNarwalEmMemoria>();

    Console.WriteLine("[SendBox] Persistencia: em memoria (defina CONNECTION_STRING_POSTGRES pra usar Postgres)");
}

// 3 - Dominio (Servicos)
construtor.Services.AddSingleton<ServicoGeradorDocumento>();

// 2 - Aplicacao (Servicos de orquestracao)
construtor.Services.AddSingleton<IServicoAplicacaoDocumento, ServicoAplicacaoDocumento>();
construtor.Services.AddSingleton<IServicoAplicacaoSessao, ServicoAplicacaoSessao>();

// 4 - Infraestrutura (Clientes externos)
construtor.Services.AddSingleton<ClienteNarwal>();

// 4 - Infraestrutura (Servicos em fundo)
construtor.Services.AddHostedService<ServicoMantenedorAtivo>();

construtor.Services.AddControllersWithViews()
    .AddJsonOptions(opcoes =>
    {
        opcoes.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var aplicacao = construtor.Build();

// Auto-migrate no startup se Postgres
if (!string.IsNullOrEmpty(connectionString))
{
    using var escopo = aplicacao.Services.CreateScope();
    var contexto = escopo.ServiceProvider.GetRequiredService<SendBoxDbContext>();
    try
    {
        contexto.Database.EnsureCreated();
        Console.WriteLine("[SendBox] Banco verificado/criado com sucesso");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SendBox] ERRO ao criar banco: {ex.Message}");
    }
}

aplicacao.UseStaticFiles();
aplicacao.UseAuthorization();
aplicacao.UseMiddleware<MiddlewareCapturaRequisicao>();
aplicacao.MapControllers();

aplicacao.Run();

// ============== HELPERS ==============

static string NormalizarConnectionString(string connectionString)
{
    // Render fornece URL no formato postgres://user:pass@host:port/db
    // Npgsql espera Host=...;Username=...;Password=...;Database=...
    if (!connectionString.StartsWith("postgres://") && !connectionString.StartsWith("postgresql://"))
        return connectionString;

    var uri = new Uri(connectionString);
    var usuarioSenha = uri.UserInfo.Split(':');
    var usuario = usuarioSenha[0];
    var senha = usuarioSenha.Length > 1 ? usuarioSenha[1] : "";
    var host = uri.Host;
    var porta = uri.Port > 0 ? uri.Port : 5432;
    var banco = uri.AbsolutePath.TrimStart('/');

    return $"Host={host};Port={porta};Database={banco};Username={usuario};Password={senha};SSL Mode=Require;Trust Server Certificate=true";
}
