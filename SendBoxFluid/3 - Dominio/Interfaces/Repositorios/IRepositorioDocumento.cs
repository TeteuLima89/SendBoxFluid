using System.Text.Json.Nodes;

namespace SendBoxFluid.Dominio.Interfaces.Repositorios;

public interface IRepositorioDocumento
{
    int ProximoCodigoEntrada();
    int ProximoNumeroLancamento();
    void Adicionar(string entidade, JsonObject documento);
    List<JsonObject> Consultar(string entidade);
    JsonObject? BuscarPorCodigoEntrada(string entidade, int codigoEntrada);
    void Limpar();
    IReadOnlyDictionary<string, IReadOnlyCollection<JsonObject>> ObterTodos();
}
