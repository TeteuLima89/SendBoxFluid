using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace SendBoxFluid.Services;

/// <summary>
/// Store em memória que guarda todos os documentos SAP B1 criados
/// via POST ou pré-populados via /sandbox/seed.
/// Singleton — compartilhado entre B1sController e SandboxController.
/// </summary>
public class DocumentStore
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<JsonObject>> _entities = new();
    private int _docEntryCounter = 1000;
    private int _jdtNumCounter = 5000;

    public int NextDocEntry() => Interlocked.Increment(ref _docEntryCounter);
    public int NextJdtNum() => Interlocked.Increment(ref _jdtNumCounter);

    public ConcurrentBag<JsonObject> GetOrCreateBag(string entity)
        => _entities.GetOrAdd(entity, _ => new ConcurrentBag<JsonObject>());

    public bool TryGetBag(string entity, out ConcurrentBag<JsonObject> bag)
        => _entities.TryGetValue(entity, out bag!);

    public void Add(string entity, JsonObject doc)
        => GetOrCreateBag(entity).Add(doc);

    public void Clear() => _entities.Clear();

    public IReadOnlyDictionary<string, ConcurrentBag<JsonObject>> All => _entities;
}
