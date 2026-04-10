using System.Text.Json.Nodes;

namespace SendBoxFluid.Domain.Interfaces;

public interface IDocumentRepository
{
    int NextDocEntry();
    int NextJdtNum();
    void Add(string entity, JsonObject doc);
    List<JsonObject> Query(string entity);
    JsonObject? FindById(string entity, int id);
    void Clear();
    IReadOnlyDictionary<string, IReadOnlyCollection<JsonObject>> GetAll();
}
