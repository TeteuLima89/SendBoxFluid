using SendBoxFluid.Aplicacao.Interfaces;
using SendBoxFluid.Aplicacao.Servicos;
using SendBoxFluid.Dominio.Interfaces.Repositorios;
using SendBoxFluid.Dominio.Servicos;
using SendBoxFluid.Infraestrutura.Middlewares;
using SendBoxFluid.Infraestrutura.Repositorios;

var construtor = WebApplication.CreateBuilder(args);

// Porta — usa PORT do ambiente (Render/IIS) ou 7078 local
var porta = Environment.GetEnvironmentVariable("PORT") ?? "7078";
construtor.WebHost.UseUrls($"http://0.0.0.0:{porta}");

// Caminho das views (estrutura customizada com pastas numeradas)
construtor.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(opcoes =>
{
    opcoes.ViewLocationFormats.Clear();
    opcoes.ViewLocationFormats.Add("/0 - Apresentacao/Views/{1}/{0}.cshtml");
    opcoes.ViewLocationFormats.Add("/0 - Apresentacao/Views/Shared/{0}.cshtml");
});

// Injecao de dependencias

// 4 - Infraestrutura (Repositorios)
construtor.Services.AddSingleton<IRepositorioDocumento, RepositorioDocumentoEmMemoria>();
construtor.Services.AddSingleton<IRepositorioSessao, RepositorioSessaoEmMemoria>();

// 3 - Dominio (Servicos)
construtor.Services.AddSingleton<ServicoGeradorDocumento>();

// 2 - Aplicacao (Servicos de orquestracao)
construtor.Services.AddSingleton<IServicoAplicacaoDocumento, ServicoAplicacaoDocumento>();
construtor.Services.AddSingleton<IServicoAplicacaoSessao, ServicoAplicacaoSessao>();

construtor.Services.AddControllersWithViews()
    .AddJsonOptions(opcoes =>
    {
        opcoes.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var aplicacao = construtor.Build();

aplicacao.UseStaticFiles();
aplicacao.UseAuthorization();

// Middleware de captura — DEVE vir antes do roteamento dos controladores
aplicacao.UseMiddleware<MiddlewareCapturaRequisicao>();

aplicacao.MapControllers();

aplicacao.Run();
