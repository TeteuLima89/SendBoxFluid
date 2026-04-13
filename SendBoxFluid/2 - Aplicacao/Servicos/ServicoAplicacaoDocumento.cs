using System.Text.Json.Nodes;
using SendBoxFluid.Aplicacao.Interfaces;
using SendBoxFluid.Dominio.Interfaces.Repositorios;
using SendBoxFluid.Dominio.Servicos;

namespace SendBoxFluid.Aplicacao.Servicos;

public class ServicoAplicacaoDocumento : IServicoAplicacaoDocumento
{
    private readonly IRepositorioDocumento _repositorioDocumento;
    private readonly ServicoGeradorDocumento _servicoGeradorDocumento;

    public ServicoAplicacaoDocumento(
        IRepositorioDocumento repositorioDocumento,
        ServicoGeradorDocumento servicoGeradorDocumento)
    {
        _repositorioDocumento = repositorioDocumento;
        _servicoGeradorDocumento = servicoGeradorDocumento;
    }

    public (int CodigoEntrada, JsonObject Documento) ReceberDocumento(string entidade, JsonObject corpo)
    {
        var ehLancamento = entidade.Equals("JournalEntries", StringComparison.OrdinalIgnoreCase);
        var codigoEntrada = ehLancamento
            ? _repositorioDocumento.ProximoNumeroLancamento()
            : _repositorioDocumento.ProximoCodigoEntrada();

        corpo["DocEntry"] = codigoEntrada;
        corpo["DocNum"] = codigoEntrada;
        if (ehLancamento)
            corpo["JdtNum"] = codigoEntrada;

        _repositorioDocumento.Adicionar(entidade, corpo);
        return (codigoEntrada, corpo);
    }

    public List<JsonObject> ConsultarComFiltro(string entidade, string? filtro, string? ordenacao, int? quantidadeMaxima)
    {
        var documentos = _repositorioDocumento.Consultar(entidade);

        if (!string.IsNullOrEmpty(filtro))
            documentos = ServicoFiltroOData.AplicarFiltro(documentos, filtro);

        // Auto-generate quando nao acha nada
        if (documentos.Count == 0 && !string.IsNullOrEmpty(filtro))
        {
            var documentoGerado = _servicoGeradorDocumento.Gerar(entidade, filtro);
            _repositorioDocumento.Adicionar(entidade, documentoGerado);
            documentos = new List<JsonObject> { documentoGerado };
        }

        if (!string.IsNullOrEmpty(ordenacao))
            documentos = ServicoFiltroOData.AplicarOrdenacao(documentos, ordenacao);

        if (quantidadeMaxima.HasValue)
            documentos = documentos.Take(quantidadeMaxima.Value).ToList();

        return documentos;
    }

    public JsonObject? ObterPorCodigoEntrada(string entidade, int codigoEntrada)
        => _repositorioDocumento.BuscarPorCodigoEntrada(entidade, codigoEntrada);

    public void Limpar() => _repositorioDocumento.Limpar();

    public IReadOnlyDictionary<string, IReadOnlyCollection<JsonObject>> ObterEstoqueCompleto()
        => _repositorioDocumento.ObterTodos();

    public void ImportarSemente(string entidade, JsonObject documento)
        => _repositorioDocumento.Adicionar(entidade, documento);
}
