using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using SendBoxFluid.Dominio.Interfaces.Repositorios;

namespace SendBoxFluid.Infraestrutura.Repositorios;

/// <summary>
/// Repositorio em memoria que guarda documentos SAP B1.
/// Singleton - dados persistem enquanto a aplicacao roda.
/// </summary>
public class RepositorioDocumentoEmMemoria : IRepositorioDocumento
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<JsonObject>> _entidades = new();
    private int _contadorCodigoEntrada = 1000;
    private int _contadorNumeroLancamento = 5000;

    public int ProximoCodigoEntrada() => Interlocked.Increment(ref _contadorCodigoEntrada);
    public int ProximoNumeroLancamento() => Interlocked.Increment(ref _contadorNumeroLancamento);

    public void Adicionar(string entidade, JsonObject documento)
    {
        var lista = _entidades.GetOrAdd(entidade, _ => new ConcurrentBag<JsonObject>());
        lista.Add(documento);
    }

    public List<JsonObject> Consultar(string entidade)
    {
        return _entidades.TryGetValue(entidade, out var lista)
            ? lista.ToList()
            : new List<JsonObject>();
    }

    public JsonObject? BuscarPorCodigoEntrada(string entidade, int codigoEntrada)
    {
        if (!_entidades.TryGetValue(entidade, out var lista))
            return null;

        return lista.FirstOrDefault(d =>
            d.TryGetPropertyValue("DocEntry", out var v) && v?.GetValue<int>() == codigoEntrada);
    }

    public void Limpar() => _entidades.Clear();

    public IReadOnlyDictionary<string, IReadOnlyCollection<JsonObject>> ObterTodos()
    {
        var resultado = new Dictionary<string, IReadOnlyCollection<JsonObject>>();
        foreach (var kvp in _entidades)
        {
            resultado[kvp.Key] = kvp.Value.ToList().AsReadOnly();
        }
        return resultado;
    }
}
