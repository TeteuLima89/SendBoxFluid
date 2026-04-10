using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using SendBoxFluid.Services;

namespace SendBoxFluid.Controllers;

[ApiController]
[Route("sandbox")]
public class SandboxController : ControllerBase
{
    private readonly DocumentStore _store;
    private readonly ILogger<SandboxController> _logger;

    public SandboxController(DocumentStore store, ILogger<SandboxController> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// POST /sandbox/seed
    /// Pré-popula dados no store para testes.
    ///
    /// Exemplo de body:
    /// {
    ///   "PurchaseOrders": [
    ///     { "DocEntry": 100, "DocNum": 100, "CardCode": "F001", "DocumentLines": [...] }
    ///   ],
    ///   "Orders": [
    ///     { "DocEntry": 200, "DocNum": 200, "CardCode": "C001", ... }
    ///   ]
    /// }
    /// </summary>
    [HttpPost("seed")]
    public IActionResult Seed([FromBody] JsonElement body)
    {
        int totalDocs = 0;

        foreach (var prop in body.EnumerateObject())
        {
            var entity = prop.Name;

            foreach (var item in prop.Value.EnumerateArray())
            {
                try
                {
                    var doc = JsonNode.Parse(item.GetRawText())?.AsObject();
                    if (doc != null)
                    {
                        _store.Add(entity, doc);
                        totalDocs++;
                    }
                }
                catch
                {
                    _logger.LogWarning("Seed: falha ao parsear doc em {Entity} (encoding)", entity);
                }
            }

            _logger.LogInformation("Seed: {Count} docs em {Entity}", prop.Value.GetArrayLength(), entity);
        }

        return Ok(new { message = $"Seed OK: {totalDocs} documentos inseridos" });
    }

    /// <summary>
    /// DELETE /sandbox/reset — Limpa tudo (entre testes).
    /// </summary>
    [HttpDelete("reset")]
    public IActionResult Reset()
    {
        _store.Clear();
        _logger.LogInformation("=== STORE RESETADO ===");
        return Ok(new { message = "Store limpo" });
    }

    /// <summary>
    /// GET /sandbox/store — Mostra tudo que está no store (debug).
    /// </summary>
    [HttpGet("store")]
    public IActionResult GetStore()
    {
        var result = new Dictionary<string, object>();
        foreach (var kvp in _store.All)
        {
            result[kvp.Key] = new { count = kvp.Value.Count, documents = kvp.Value.ToList() };
        }
        return Ok(result);
    }

    /// <summary>
    /// GET /sandbox/store/{entity} — Mostra docs de uma entidade.
    /// </summary>
    [HttpGet("store/{entity}")]
    public IActionResult GetStoreEntity(string entity)
    {
        if (_store.TryGetBag(entity, out var bag))
            return Ok(new { count = bag.Count, documents = bag.ToList() });
        return Ok(new { count = 0, documents = Array.Empty<object>() });
    }
}
