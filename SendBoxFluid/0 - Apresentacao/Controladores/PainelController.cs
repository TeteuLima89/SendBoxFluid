using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SendBoxFluid.Aplicacao.Interfaces;
using SendBoxFluid.Apresentacao.ViewModels;
using SendBoxFluid.Dominio.Servicos;

namespace SendBoxFluid.Apresentacao.Controladores;

/// <summary>
/// Controlador do painel de visualizacao para o QA.
/// </summary>
public class PainelController : Controller
{
    private readonly IServicoAplicacaoSessao _servicoAplicacaoSessao;

    public PainelController(IServicoAplicacaoSessao servicoAplicacaoSessao)
    {
        _servicoAplicacaoSessao = servicoAplicacaoSessao;
    }

    [HttpGet("/")]
    [HttpGet("/painel")]
    public IActionResult Index()
    {
        var sessoes = _servicoAplicacaoSessao.ListarTodas();
        var modelo = sessoes.Select(s => new SessaoListaViewModel
        {
            CodigoSessao = s.CodigoSessao,
            DataInicio = s.DataInicio,
            DataUltimaAtividade = s.DataUltimaAtividade,
            TipoAcao = ServicoIdentificadorAcao.ObterDescricao(s.TipoAcao),
            Resultado = s.Resultado.ToString(),
            Mensagem = s.Mensagem,
            QuantidadeRequisicoes = s.Requisicoes.Count
        }).ToList();

        return View(modelo);
    }

    [HttpGet("/painel/sessao/{codigoSessao}")]
    public IActionResult Detalhe(string codigoSessao)
    {
        var sessao = _servicoAplicacaoSessao.ObterPorCodigo(codigoSessao);
        if (sessao == null)
            return NotFound();

        var relatorio = _servicoAplicacaoSessao.ConstruirRelatorio(codigoSessao);
        var modelo = new SessaoDetalheViewModel
        {
            CodigoSessao = sessao.CodigoSessao,
            DataInicio = sessao.DataInicio,
            DataUltimaAtividade = sessao.DataUltimaAtividade,
            TipoAcao = ServicoIdentificadorAcao.ObterDescricao(sessao.TipoAcao),
            Resultado = sessao.Resultado.ToString(),
            Mensagem = sessao.Mensagem,
            Requisicoes = sessao.Requisicoes.Select(r => new RequisicaoViewModel
            {
                Identificador = r.Identificador,
                DataHora = r.DataHora,
                Metodo = r.Metodo,
                Caminho = r.Caminho,
                Entidade = r.Entidade,
                CodigoStatusHttp = r.CodigoStatusHttp,
                CorpoRequisicao = FormatarJson(r.CorpoRequisicao),
                CorpoResposta = FormatarJson(r.CorpoResposta)
            }).ToList(),
            JsonRelatorio = relatorio == null ? "{}" : FormatarObjeto(relatorio)
        };

        return View(modelo);
    }

    [HttpGet("/painel/sessao/{codigoSessao}/download/relatorio")]
    public IActionResult BaixarRelatorio(string codigoSessao)
    {
        var relatorio = _servicoAplicacaoSessao.ConstruirRelatorio(codigoSessao);
        if (relatorio == null)
            return NotFound();

        var conteudo = FormatarObjeto(relatorio);
        var bytes = Encoding.UTF8.GetBytes(conteudo);
        var nomeArquivo = $"relatorio-{codigoSessao[..8]}.json";
        return File(bytes, "application/json", nomeArquivo);
    }

    [HttpGet("/painel/sessao/{codigoSessao}/download/payload")]
    public IActionResult BaixarPayload(string codigoSessao)
    {
        var sessao = _servicoAplicacaoSessao.ObterPorCodigo(codigoSessao);
        if (sessao?.PayloadEnviadoErp == null)
            return NotFound();

        var conteudo = FormatarJson(sessao.PayloadEnviadoErp);
        var bytes = Encoding.UTF8.GetBytes(conteudo);
        var nomeArquivo = $"payload-{codigoSessao[..8]}.json";
        return File(bytes, "application/json", nomeArquivo);
    }

    private static string FormatarJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try
        {
            using var documento = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(documento, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    private static string FormatarObjeto(object obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        });
    }
}
