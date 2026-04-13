using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using SendBoxFluid.Aplicacao.Interfaces;

namespace SendBoxFluid.Apresentacao.Controladores;

[ApiController]
[Route("sandbox")]
public class SandboxController : ControllerBase
{
    private readonly IServicoAplicacaoDocumento _servicoAplicacaoDocumento;
    private readonly IServicoAplicacaoSessao _servicoAplicacaoSessao;
    private readonly ILogger<SandboxController> _registradorEventos;

    public SandboxController(
        IServicoAplicacaoDocumento servicoAplicacaoDocumento,
        IServicoAplicacaoSessao servicoAplicacaoSessao,
        ILogger<SandboxController> registradorEventos)
    {
        _servicoAplicacaoDocumento = servicoAplicacaoDocumento;
        _servicoAplicacaoSessao = servicoAplicacaoSessao;
        _registradorEventos = registradorEventos;
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
