using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using SendBoxFluid.Domain.Interfaces;
using SendBoxFluid.Domain.Services;

namespace SendBoxFluid.Controllers;

[ApiController]
[Route("b1s")]
public class B1sController : ControllerBase
{
    private readonly IDocumentRepository _repository;
    private readonly DocumentGeneratorService _generator;
    private readonly ILogger<B1sController> _logger;

    public B1sController(
        IDocumentRepository repository,
        DocumentGeneratorService generator,
        ILogger<B1sController> logger)
    {
        _repository = repository;
        _generator = generator;
        _logger = logger;
    }

    [HttpPost("v1/Login")]
    public IActionResult PostLogin()
    {
        _logger.LogInformation("=== LOGIN ===");
        return Ok(new
        {
            SessionId = Guid.NewGuid().ToString(),
            SessionTimeout = 30,
            Version = "1000300"
        });
    }

    [HttpPost("v1/Logout")]
    public IActionResult PostLogout()
    {
        _logger.LogInformation("=== LOGOUT ===");
        return NoContent();
    }

    [HttpPost("v1/{entity}")]
    public async Task<IActionResult> PostEntity(string entity)
    {
        var isJournal = entity.Equals("JournalEntries", StringComparison.OrdinalIgnoreCase);
        var docEntry = isJournal ? _repository.NextJdtNum() : _repository.NextDocEntry();

        _logger.LogInformation("=== POST /b1s/v1/{Entity} === DocEntry={DocEntry}", entity, docEntry);

        var doc = await ReadBodyAsJson();
        doc["DocEntry"] = docEntry;
        doc["DocNum"] = docEntry;
        if (isJournal)
            doc["JdtNum"] = docEntry;

        _repository.Add(entity, doc);

        if (isJournal)
            return Created($"/b1s/v1/{entity}({docEntry})", new { JdtNum = docEntry, DocEntry = docEntry, DocNum = docEntry });

        return Created($"/b1s/v1/{entity}({docEntry})", new { DocEntry = docEntry, DocNum = docEntry, DocTotal = 0, DocTotalFc = 0 });
    }

    [HttpPost("v1/{entity}({id})/Cancel")]
    public IActionResult PostCancel(string entity, int id)
    {
        _logger.LogInformation("=== CANCEL /b1s/v1/{Entity}({Id}) ===", entity, id);
        return NoContent();
    }

    [HttpPatch("v1/{entity}({id})")]
    [HttpPatch("v2/{entity}({id})")]
    public IActionResult PatchEntity(string entity, int id)
    {
        _logger.LogInformation("=== PATCH /b1s/{Entity}({Id}) ===", entity, id);
        return NoContent();
    }

    [HttpGet("v1/{entity}")]
    public IActionResult GetEntity(
        string entity,
        [FromQuery(Name = "$filter")] string? filter,
        [FromQuery(Name = "$select")] string? select,
        [FromQuery(Name = "$orderby")] string? orderby,
        [FromQuery(Name = "$top")] int? top)
    {
        filter = ResolveQueryParam(filter, "$filter");
        select = ResolveQueryParam(select, "$select");
        orderby = ResolveQueryParam(orderby, "$orderby");

        _logger.LogInformation("=== GET /b1s/v1/{Entity} === $filter={Filter}", entity, filter);

        var docs = _repository.Query(entity);

        if (!string.IsNullOrEmpty(filter))
            docs = ODataFilterService.ApplyFilter(docs, filter);

        // Auto-generate quando não encontra nada
        if (docs.Count == 0 && !string.IsNullOrEmpty(filter))
        {
            var generated = _generator.Generate(entity, filter);
            _repository.Add(entity, generated);
            docs = new List<JsonObject> { generated };
            _logger.LogInformation("Auto-generated {Entity} from filter", entity);
        }

        if (!string.IsNullOrEmpty(orderby))
            docs = ODataFilterService.ApplyOrderBy(docs, orderby);

        if (top.HasValue)
            docs = docs.Take(top.Value).ToList();

        if (!string.IsNullOrEmpty(select))
        {
            var fields = select.Split(',').Select(f => f.Trim()).ToHashSet();
            var filtered = docs.Select(d => ODataFilterService.FilterFields(d, fields)).ToList();
            return Ok(new { value = filtered });
        }

        return Ok(new { value = docs });
    }

    [HttpGet("v1/{entity}({id})")]
    public IActionResult GetEntityById(string entity, int id)
    {
        _logger.LogInformation("=== GET /b1s/v1/{Entity}({Id}) ===", entity, id);

        var doc = _repository.FindById(entity, id);
        if (doc != null)
            return Ok(doc);

        return NotFound(new
        {
            error = new { code = -2028, message = new { value = $"No matching records found for {entity}({id})" } }
        });
    }

    // =====================
    // Helpers privados
    // =====================

    /// <summary>
    /// O Fluid codifica $filter=valor como %24filter%3Dvalor.
    /// ASP.NET não parseia. Extrai da raw query string como fallback.
    /// </summary>
    private string? ResolveQueryParam(string? value, string paramName)
    {
        if (!string.IsNullOrEmpty(value))
            return value;

        var raw = Uri.UnescapeDataString(Request.QueryString.Value ?? "");
        var prefix = paramName + "=";
        var idx = raw.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            // Tenta sem o $
            prefix = paramName.TrimStart('$') + "=";
            idx = raw.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        }
        if (idx < 0) return null;

        var start = idx + prefix.Length;
        var end = raw.IndexOf('&', start);
        return end < 0 ? raw[start..] : raw[start..end];
    }

    private async Task<JsonObject> ReadBodyAsJson()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var raw = await reader.ReadToEndAsync();
            _logger.LogInformation("Payload:\n{Body}", raw);
            return JsonNode.Parse(raw)?.AsObject() ?? new JsonObject();
        }
        catch
        {
            _logger.LogInformation("Payload: (vazio ou não-JSON)");
            return new JsonObject();
        }
    }
}
