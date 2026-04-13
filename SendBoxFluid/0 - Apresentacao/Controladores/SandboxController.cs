using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using SendBoxFluid.Aplicacao.Interfaces;
using SendBoxFluid.Dominio.Entidades;
using SendBoxFluid.Dominio.Interfaces.Repositorios;

namespace SendBoxFluid.Apresentacao.Controladores;

[ApiController]
[Route("sandbox")]
public class SandboxController : ControllerBase
{
    private readonly IServicoAplicacaoDocumento _servicoAplicacaoDocumento;
    private readonly IServicoAplicacaoSessao _servicoAplicacaoSessao;
    private readonly IRepositorioConfiguracaoNarwal _repositorioConfiguracaoNarwal;
    private readonly ILogger<SandboxController> _registradorEventos;

    public SandboxController(
        IServicoAplicacaoDocumento servicoAplicacaoDocumento,
        IServicoAplicacaoSessao servicoAplicacaoSessao,
        IRepositorioConfiguracaoNarwal repositorioConfiguracaoNarwal,
        ILogger<SandboxController> registradorEventos)
    {
        _servicoAplicacaoDocumento = servicoAplicacaoDocumento;
        _servicoAplicacaoSessao = servicoAplicacaoSessao;
        _repositorioConfiguracaoNarwal = repositorioConfiguracaoNarwal;
        _registradorEventos = registradorEventos;
    }

    /// <summary>
    /// Configura acesso ao Narwal pra enriquecer sessoes com dados originais.
    /// </summary>
    [HttpPost("configurar-narwal")]
    public IActionResult ConfigurarNarwal([FromBody] ConfiguracaoNarwal configuracao)
    {
        _repositorioConfiguracaoNarwal.Salvar(configuracao);
        _registradorEventos.LogInformation("Configuracao Narwal salva: {Cliente} -> {Url}",
            configuracao.Cliente, configuracao.UrlNarwal);
        return Ok(new { mensagem = "Configuracao salva", cliente = configuracao.Cliente });
    }

    [HttpGet("configuracoes-narwal")]
    public IActionResult ListarConfiguracoesNarwal()
    {
        var lista = _repositorioConfiguracaoNarwal.ListarTodas()
            .Select(c => new { c.Cliente, c.UrlNarwal, c.Usuario });
        return Ok(lista);
    }

    [HttpPost("seed")]
    public IActionResult Semear([FromBody] JsonElement corpo)
    {
        int totalDocumentos = 0;

        foreach (var propriedade in corpo.EnumerateObject())
        {
            var entidade = propriedade.Name;
            foreach (var item in propriedade.Value.EnumerateArray())
            {
                try
                {
                    var documento = JsonNode.Parse(item.GetRawText())?.AsObject();
                    if (documento != null)
                    {
                        _servicoAplicacaoDocumento.ImportarSemente(entidade, documento);
                        totalDocumentos++;
                    }
                }
                catch
                {
                    _registradorEventos.LogWarning("Falha ao parsear semente em {Entidade}", entidade);
                }
            }
        }

        return Ok(new { mensagem = $"Semente OK: {totalDocumentos} documentos inseridos" });
    }

    [HttpDelete("reset")]
    public IActionResult Resetar()
    {
        _servicoAplicacaoDocumento.Limpar();
        _servicoAplicacaoSessao.Limpar();
        _registradorEventos.LogInformation("=== STORE RESETADO ===");
        return Ok(new { mensagem = "Store limpo" });
    }

    /// <summary>
    /// Endpoint de saude usado pelo auto-ping pra manter o Render free tier acordado.
    /// </summary>
    [HttpGet("/saude")]
    public IActionResult VerificarSaude() => Ok(new
    {
        status = "ativo",
        dataHora = DateTime.UtcNow,
        sessoes = _servicoAplicacaoSessao.ListarTodas().Count
    });

    [HttpGet("store")]
    public IActionResult ObterEstoque()
    {
        var todos = _servicoAplicacaoDocumento.ObterEstoqueCompleto();
        var resultado = new Dictionary<string, object>();
        foreach (var kvp in todos)
        {
            resultado[kvp.Key] = new { quantidade = kvp.Value.Count, documentos = kvp.Value };
        }
        return Ok(resultado);
    }
}
