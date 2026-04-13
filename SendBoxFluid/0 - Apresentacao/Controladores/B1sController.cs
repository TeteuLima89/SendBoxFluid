using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using SendBoxFluid.Aplicacao.Interfaces;
using SendBoxFluid.Dominio.Servicos;

namespace SendBoxFluid.Apresentacao.Controladores;

[ApiController]
[Route("b1s")]
public class B1sController : ControllerBase
{
    private readonly IServicoAplicacaoDocumento _servicoAplicacaoDocumento;
    private readonly ILogger<B1sController> _registradorEventos;

    public B1sController(
        IServicoAplicacaoDocumento servicoAplicacaoDocumento,
        ILogger<B1sController> registradorEventos)
    {
        _servicoAplicacaoDocumento = servicoAplicacaoDocumento;
        _registradorEventos = registradorEventos;
    }

    [HttpPost("v1/Login")]
    public IActionResult RealizarLogin()
    {
        _registradorEventos.LogInformation("=== LOGIN ===");
        return Ok(new
        {
            SessionId = Guid.NewGuid().ToString(),
            SessionTimeout = 30,
            Version = "1000300"
        });
    }

    [HttpPost("v1/Logout")]
    public IActionResult RealizarLogout()
    {
        _registradorEventos.LogInformation("=== LOGOUT ===");
        return NoContent();
    }

    [HttpPost("v1/{entidade}")]
    public async Task<IActionResult> CriarDocumento(string entidade)
    {
        var corpo = await LerCorpoComoJson();
        var (codigoEntrada, _) = _servicoAplicacaoDocumento.ReceberDocumento(entidade, corpo);

        _registradorEventos.LogInformation("POST /b1s/v1/{Entidade} -> DocEntry={CodigoEntrada}", entidade, codigoEntrada);

        // Estrutura "error" sempre presente (vazia quando sucesso) para evitar
        // nil pointer no template Go do passo "retornoerro" do Fluid.
        // O template faz: index .steps.envia-nota-fiscal.body.error.message.value
        // Se nao existir, quebra com "index of nil pointer".
        var erroVazio = new
        {
            code = 0,
            message = new { value = "", lang = "pt-BR" }
        };

        if (entidade.Equals("JournalEntries", StringComparison.OrdinalIgnoreCase))
        {
            return Created($"/b1s/v1/{entidade}({codigoEntrada})", new
            {
                JdtNum = codigoEntrada,
                DocEntry = codigoEntrada,
                DocNum = codigoEntrada,
                error = erroVazio
            });
        }

        return Created($"/b1s/v1/{entidade}({codigoEntrada})", new
        {
            DocEntry = codigoEntrada,
            DocNum = codigoEntrada,
            DocTotal = 0,
            DocTotalFc = 0,
            error = erroVazio
        });
    }

    [HttpPost("v1/{entidade}({codigoEntrada})/Cancel")]
    public IActionResult CancelarDocumento(string entidade, int codigoEntrada)
    {
        _registradorEventos.LogInformation("CANCEL /b1s/v1/{Entidade}({Codigo})", entidade, codigoEntrada);
        return NoContent();
    }

    [HttpPatch("v1/{entidade}({codigoEntrada})")]
    [HttpPatch("v2/{entidade}({codigoEntrada})")]
    public IActionResult AtualizarDocumento(string entidade, int codigoEntrada)
    {
        _registradorEventos.LogInformation("PATCH /b1s/{Entidade}({Codigo})", entidade, codigoEntrada);
        return NoContent();
    }

    [HttpGet("v1/{entidade}")]
    public IActionResult ConsultarEntidade(
        string entidade,
        [FromQuery(Name = "$filter")] string? filtro,
        [FromQuery(Name = "$select")] string? selecao,
        [FromQuery(Name = "$orderby")] string? ordenacao,
        [FromQuery(Name = "$top")] int? quantidadeMaxima)
    {
        filtro = ResolverParametroQuery(filtro, "$filter");
        selecao = ResolverParametroQuery(selecao, "$select");
        ordenacao = ResolverParametroQuery(ordenacao, "$orderby");

        _registradorEventos.LogInformation("GET /b1s/v1/{Entidade} $filter={Filtro}", entidade, filtro);

        var documentos = _servicoAplicacaoDocumento.ConsultarComFiltro(entidade, filtro, ordenacao, quantidadeMaxima);

        if (!string.IsNullOrEmpty(selecao))
        {
            var campos = selecao.Split(',').Select(c => c.Trim()).ToHashSet();
            var filtrados = documentos.Select(d => ServicoFiltroOData.FiltrarCampos(d, campos)).ToList();
            return Ok(new { value = filtrados });
        }

        return Ok(new { value = documentos });
    }

    [HttpGet("v1/{entidade}({codigoEntrada})")]
    public IActionResult ConsultarPorCodigoEntrada(string entidade, int codigoEntrada)
    {
        var documento = _servicoAplicacaoDocumento.ObterPorCodigoEntrada(entidade, codigoEntrada);
        if (documento != null)
            return Ok(documento);

        return NotFound(new
        {
            error = new { code = -2028, message = new { value = $"No matching records found for {entidade}({codigoEntrada})" } }
        });
    }

    /// <summary>
    /// O Fluid codifica $filter=valor como %24filter%3Dvalor.
    /// ASP.NET nao parseia. Extrai da raw query string como fallback.
    /// </summary>
    private string? ResolverParametroQuery(string? valor, string nomeParametro)
    {
        if (!string.IsNullOrEmpty(valor))
            return valor;

        var bruto = Uri.UnescapeDataString(Request.QueryString.Value ?? "");
        var prefixo = nomeParametro + "=";
        var indice = bruto.IndexOf(prefixo, StringComparison.OrdinalIgnoreCase);
        if (indice < 0)
        {
            prefixo = nomeParametro.TrimStart('$') + "=";
            indice = bruto.IndexOf(prefixo, StringComparison.OrdinalIgnoreCase);
        }
        if (indice < 0) return null;

        var inicio = indice + prefixo.Length;
        var fim = bruto.IndexOf('&', inicio);
        return fim < 0 ? bruto[inicio..] : bruto[inicio..fim];
    }

    private async Task<JsonObject> LerCorpoComoJson()
    {
        try
        {
            using var leitor = new StreamReader(Request.Body);
            var bruto = await leitor.ReadToEndAsync();
            return JsonNode.Parse(bruto)?.AsObject() ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }
}
