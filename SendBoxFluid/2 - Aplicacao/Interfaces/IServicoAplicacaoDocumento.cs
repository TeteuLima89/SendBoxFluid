using System.Text.Json.Nodes;

namespace SendBoxFluid.Aplicacao.Interfaces;

public interface IServicoAplicacaoDocumento
{
    (int CodigoEntrada, JsonObject Documento) ReceberDocumento(string entidade, JsonObject corpo);
    List<JsonObject> ConsultarComFiltro(string entidade, string? filtro, string? ordenacao, int? quantidadeMaxima);
    JsonObject? ObterPorCodigoEntrada(string entidade, int codigoEntrada);
    void Limpar();
    IReadOnlyDictionary<string, IReadOnlyCollection<JsonObject>> ObterEstoqueCompleto();
    void ImportarSemente(string entidade, JsonObject documento);
}
