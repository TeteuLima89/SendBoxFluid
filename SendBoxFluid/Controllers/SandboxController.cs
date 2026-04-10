using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using SendBoxFluid.Domain.Interfaces;

namespace SendBoxFluid.Controllers;

[ApiController]
[Route("sandbox")]
public class SandboxController : ControllerBase
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<SandboxController> _logger;

    public SandboxController(IDocumentRepository repository, ILogger<SandboxController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

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
                        _repository.Add(entity, doc);
                        totalDocs++;
                    }
                }
                catch
                {
                    _logger.LogWarning("Seed: falha ao parsear doc em {Entity}", entity);
                }
            }
        }

        return Ok(new { message = $"Seed OK: {totalDocs} documentos inseridos" });
    }

    [HttpDelete("reset")]
    public IActionResult Reset()
    {
        _repository.Clear();
        _logger.LogInformation("=== STORE RESETADO ===");
        return Ok(new { message = "Store limpo" });
    }

    [HttpGet("store")]
    public IActionResult GetStore()
    {
        var all = _repository.GetAll();
        var result = new Dictionary<string, object>();
        foreach (var kvp in all)
        {
            result[kvp.Key] = new { count = kvp.Value.Count, documents = kvp.Value };
        }
        return Ok(result);
    }

    [HttpGet("store/{entity}")]
    public IActionResult GetStoreEntity(string entity)
    {
        var docs = _repository.Query(entity);
        return Ok(new { count = docs.Count, documents = docs });
    }
}
