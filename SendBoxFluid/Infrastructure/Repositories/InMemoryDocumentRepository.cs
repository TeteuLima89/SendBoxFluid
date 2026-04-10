using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using SendBoxFluid.Domain.Interfaces;

namespace SendBoxFluid.Infrastructure.Repositories;

/// <summary>
/// Repositório em memória que guarda documentos SAP B1.
/// Singleton — dados persistem enquanto a aplicação roda.
/// </summary>
public class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<JsonObject>> _entities = new();
    private int _docEntryCounter = 1000;
    private int _jdtNumCounter = 5000;

    public int NextDocEntry() => Interlocked.Increment(ref _docEntryCounter);
    public int NextJdtNum() => Interlocked.Increment(ref _jdtNumCounter);

    public void Add(string entity, JsonObject doc)
    {
        var bag = _entities.GetOrAdd(entity, _ => new ConcurrentBag<JsonObject>());
        bag.Add(doc);
    }

    public List<JsonObject> Query(string entity)
    {
        return _entities.TryGetValue(entity, out var bag)
            ? bag.ToList()
            : new List<JsonObject>();
    }

    public JsonObject? FindById(string entity, int id)
    {
        if (!_entities.TryGetValue(entity, out var bag))
            return null;

        return bag.FirstOrDefault(d =>
            d.TryGetPropertyValue("DocEntry", out var v) && v?.GetValue<int>() == id);
    }

    public void Clear() => _entities.Clear();

    public IReadOnlyDictionary<string, IReadOnlyCollection<JsonObject>> GetAll()
    {
        var result = new Dictionary<string, IReadOnlyCollection<JsonObject>>();
        foreach (var kvp in _entities)
        {
            result[kvp.Key] = kvp.Value.ToList().AsReadOnly();
        }
        return result;
    }
}
