using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SendBoxFluid.Aplicacao.Interfaces;
using SendBoxFluid.Apresentacao.ViewModels;
using SendBoxFluid.Dominio.Entidades;
using SendBoxFluid.Dominio.Enumeradores;
using SendBoxFluid.Dominio.Interfaces.Repositorios;
using SendBoxFluid.Dominio.Servicos;

namespace SendBoxFluid.Apresentacao.Controladores;

/// <summary>
/// Controlador do painel de visualizacao para o QA.
/// </summary>
public class PainelController : Controller
{
    private readonly IServicoAplicacaoSessao _servicoAplicacaoSessao;
    private readonly IRepositorioConfiguracaoNarwal _repositorioConfiguracaoNarwal;

    public PainelController(
        IServicoAplicacaoSessao servicoAplicacaoSessao,
        IRepositorioConfiguracaoNarwal repositorioConfiguracaoNarwal)
    {
        _servicoAplicacaoSessao = servicoAplicacaoSessao;
        _repositorioConfiguracaoNarwal = repositorioConfiguracaoNarwal;
    }

    [HttpGet("/painel/configuracao")]
    public IActionResult Configuracao()
    {
        ViewData["TotalGeral"] = _servicoAplicacaoSessao.ListarTodas().Count;
        return View(_repositorioConfiguracaoNarwal.ListarTodas());
    }

    [HttpPost("/painel/configuracao/narwal/salvar")]
    public IActionResult SalvarConfiguracaoNarwal(string cliente, string urlNarwal, string usuario, string senha)
    {
        if (string.IsNullOrWhiteSpace(cliente) || string.IsNullOrWhiteSpace(urlNarwal))
        {
            TempData["MensagemErro"] = "Cliente e URL sao obrigatorios";
            return RedirectToAction(nameof(Configuracao));
        }

        _repositorioConfiguracaoNarwal.Salvar(new ConfiguracaoNarwal
        {
            Cliente = cliente,
            UrlNarwal = urlNarwal,
            Usuario = string.IsNullOrWhiteSpace(usuario) ? "integracao" : usuario,
            Senha = senha ?? string.Empty
        });

        TempData["MensagemSucesso"] = $"Configuracao salva para {cliente}";
        return RedirectToAction(nameof(Configuracao));
    }

    [HttpPost("/painel/configuracao/narwal/remover")]
    public IActionResult RemoverConfiguracaoNarwal(string cliente)
    {
        _repositorioConfiguracaoNarwal.Remover(cliente);
        TempData["MensagemSucesso"] = $"Configuracao removida: {cliente}";
        return RedirectToAction(nameof(Configuracao));
    }

    [HttpGet("/")]
    public IActionResult Inicio() => RedirectToAction(nameof(Index), new { erp = "todos" });

    [HttpGet("/painel")]
    [HttpGet("/painel/{erp}")]
    public IActionResult Index(string erp = "todos")
    {
        var todasSessoes = _servicoAplicacaoSessao.ListarTodas();

        // Filtra fora as sessoes que so fizeram autenticacao (sao ruido,
        // nao representam integracao real). Mostra so as que tem POST principal.
        var sessoesRelevantes = todasSessoes
            .Where(s => s.Resultado != ResultadoIntegracaoEnum.EmAndamento ||
                        s.Requisicoes.Any(r =>
                            r.Metodo.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                            !r.Caminho.Contains("/Login", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var sessoesFiltradas = erp.ToLower() switch
        {
            "sapb1" or "sap-b1" => sessoesRelevantes.Where(s => s.TipoErp == TipoErpEnum.SapB1),
            "sankhya" => sessoesRelevantes.Where(s => s.TipoErp == TipoErpEnum.Sankhya),
            _ => sessoesRelevantes
        };

        var modelo = sessoesFiltradas.Select(s => new SessaoListaViewModel
        {
            CodigoSessao = s.CodigoSessao,
            DataInicio = s.DataInicio,
            DataUltimaAtividade = s.DataUltimaAtividade,
            TipoAcao = ServicoIdentificadorAcao.ObterDescricao(s.TipoAcao),
            TipoErp = s.TipoErp.ToString(),
            Resultado = s.Resultado.ToString(),
            Mensagem = s.Mensagem,
            IdentificadorNegocio = s.IdentificadorNegocio,
            QuantidadeRequisicoes = s.Requisicoes.Count
        }).ToList();

        ViewData["ErpAtivo"] = erp.ToLower();
        ViewData["TotalGeral"] = sessoesRelevantes.Count;
        ViewData["TotalSapB1"] = sessoesRelevantes.Count(s => s.TipoErp == TipoErpEnum.SapB1);
        ViewData["TotalSankhya"] = sessoesRelevantes.Count(s => s.TipoErp == TipoErpEnum.Sankhya);

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
            IdentificadorNegocio = sessao.IdentificadorNegocio,
            DadosOriginaisNarwal = string.IsNullOrEmpty(sessao.DadosOriginaisNarwal)
                ? null
                : FormatarJson(sessao.DadosOriginaisNarwal),
            // Mostra todas as requisicoes exceto Login (que e so autenticacao)
            Requisicoes = sessao.Requisicoes
                .Where(r => !r.Caminho.Contains("/Login", StringComparison.OrdinalIgnoreCase))
                .Select(r => new RequisicaoViewModel
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
        if (relatorio == null) return NotFound();

        var conteudo = FormatarObjeto(relatorio);
        var bytes = Encoding.UTF8.GetBytes(conteudo);
        return File(bytes, "application/json", $"relatorio-{codigoSessao[..8]}.json");
    }

    [HttpGet("/painel/sessao/{codigoSessao}/download/payload")]
    public IActionResult BaixarPayload(string codigoSessao)
    {
        var sessao = _servicoAplicacaoSessao.ObterPorCodigo(codigoSessao);
        if (sessao?.PayloadEnviadoErp == null) return NotFound();

        var conteudo = FormatarJson(sessao.PayloadEnviadoErp);
        var bytes = Encoding.UTF8.GetBytes(conteudo);
        return File(bytes, "application/json", $"payload-{codigoSessao[..8]}.json");
    }

    private static string FormatarJson(string json) => ServicoFormatadorJson.Formatar(json);

    private static string FormatarObjeto(object obj) => JsonSerializer.Serialize(obj, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    });
}
