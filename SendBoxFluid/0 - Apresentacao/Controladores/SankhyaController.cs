using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace SendBoxFluid.Apresentacao.Controladores;

/// <summary>
/// Mock dos endpoints do Sankhya ERP.
/// Aceita qualquer serviceName e retorna sucesso generico.
/// </summary>
[ApiController]
public class SankhyaController : ControllerBase
{
    private static int _contadorChave = 10000;
    private readonly ILogger<SankhyaController> _registradorEventos;

    public SankhyaController(ILogger<SankhyaController> registradorEventos)
    {
        _registradorEventos = registradorEventos;
    }

    /// <summary>
    /// Endpoint principal do Sankhya - todos os servicos passam por aqui.
    /// Ex: /mge/service.sbr?serviceName=MobileLoginSP.login
    ///     /mge/service.sbr?serviceName=CRUDServiceProvider.saveRecord
    ///     /mge/service.sbr?serviceName=CACSP.incluirNota
    /// </summary>
    [HttpPost("/mge/service.sbr")]
    [HttpGet("/mge/service.sbr")]
    public async Task<IActionResult> ProcessarServico([FromQuery] string serviceName)
    {
        _registradorEventos.LogInformation("=== Sankhya {Servico} ===", serviceName);

        var corpo = await LerCorpoComoTexto();
        var chave = Interlocked.Increment(ref _contadorChave);

        // Roteia por tipo de servico
        return serviceName switch
        {
            "MobileLoginSP.login" => RetornarLogin(),
            "CRUDServiceProvider.saveRecord" => RetornarSaveRecord(chave),
            "CACSP.incluirNota" => RetornarIncluirNota(chave),
            "CACSP.incluirAlterarItemNota" => RetornarSucessoGenerico(chave),
            "CACSP.excluirNotas" => RetornarSucessoGenerico(chave),
            "BaixaFinanceiroSP.baixarTitulo" => RetornarSucessoGenerico(chave),
            "DbExplorerSP.executeQuery" => RetornarConsulta(),
            _ => RetornarSucessoGenerico(chave)
        };
    }

    /// <summary>
    /// Endpoint do Gateway Sankhya (alternativa ao /mge).
    /// </summary>
    [HttpPost("/sankhya/{*path}")]
    public async Task<IActionResult> ProcessarGateway(string path)
    {
        _registradorEventos.LogInformation("=== Sankhya Gateway /{Caminho} ===", path);
        await LerCorpoComoTexto(); // Le mas nao usa
        var chave = Interlocked.Increment(ref _contadorChave);

        if (path.Contains("login", StringComparison.OrdinalIgnoreCase))
            return RetornarLoginGateway();

        return RetornarSucessoGateway(chave);
    }

    // ============== Respostas mock ==============

    private static IActionResult RetornarLogin()
    {
        // Formato Sankhya: { responseBody: { jsessionid: { $: "ID" }, ... } }
        var resposta = new
        {
            responseBody = new
            {
                jsessionid = new { _ = "JSESSION-" + Guid.NewGuid().ToString() },
                idusu = new { _ = "1" },
                callID = new { _ = "1" }
            }
        };
        return new OkObjectResult(resposta);
    }

    private static IActionResult RetornarLoginGateway()
    {
        return new OkObjectResult(new
        {
            token = "GATEWAY-" + Guid.NewGuid().ToString()
        });
    }

    private static IActionResult RetornarSaveRecord(int chave)
    {
        return new OkObjectResult(new
        {
            serviceName = "CRUDServiceProvider.saveRecord",
            status = "1",
            responseBody = new
            {
                pk = new { NUFIN = new { _ = chave.ToString() } }
            }
        });
    }

    private static IActionResult RetornarIncluirNota(int chave)
    {
        return new OkObjectResult(new
        {
            serviceName = "CACSP.incluirNota",
            status = "1",
            responseBody = new
            {
                NunotaSalva = chave.ToString(),
                NumNota = chave.ToString()
            }
        });
    }

    private static IActionResult RetornarConsulta()
    {
        return new OkObjectResult(new
        {
            serviceName = "DbExplorerSP.executeQuery",
            status = "1",
            responseBody = new
            {
                fieldsMetadata = new[]
                {
                    new { name = "NUFIN" },
                    new { name = "DESCRICAO" }
                },
                rows = Array.Empty<object>()
            }
        });
    }

    private static IActionResult RetornarSucessoGenerico(int chave)
    {
        return new OkObjectResult(new
        {
            status = "1",
            responseBody = new
            {
                chave = chave.ToString(),
                pk = new { _ = chave.ToString() }
            }
        });
    }

    private static IActionResult RetornarSucessoGateway(int chave)
    {
        return new OkObjectResult(new
        {
            sucesso = true,
            chave = chave,
            mensagem = "Operacao realizada com sucesso"
        });
    }

    private async Task<string> LerCorpoComoTexto()
    {
        try
        {
            using var leitor = new StreamReader(Request.Body);
            return await leitor.ReadToEndAsync();
        }
        catch
        {
            return string.Empty;
        }
    }
}
