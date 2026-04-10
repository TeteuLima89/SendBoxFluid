using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using SendBoxFluid.Models.Login;
using SendBoxFluid.Services;

namespace SendBoxFluid.Controllers;

[ApiController]
[Route("b1s")]
public class B1sController : ControllerBase
{
    private readonly DocumentStore _store;
    private readonly ILogger<B1sController> _logger;

    public B1sController(DocumentStore store, ILogger<B1sController> logger)
    {
        _store = store;
        _logger = logger;
    }

    // =========================
    // POST /b1s/v1/Login
    // =========================
    [HttpPost("v1/Login")]
    public LoginResponse PostLogin(LoginRequest request)
    {
        _logger.LogInformation("=== LOGIN === CompanyDB={CompanyDB} UserName={UserName}",
            request.CompanyDB, request.UserName);

        return new LoginResponse
        {
            SessionId = Guid.NewGuid().ToString(),
            SessionTimeout = 30,
            Version = "1000300"
        };
    }

    // =========================
    // POST /b1s/v1/Logout
    // =========================
    [HttpPost("v1/Logout")]
    public IActionResult PostLogout()
    {
        _logger.LogInformation("=== LOGOUT ===");
        return NoContent();
    }

    // ==========================================================
    // POST /b1s/v1/{entity}
    // Endpoint dinâmico — aceita QUALQUER entidade do SAP B1.
    // Guarda no store e retorna DocEntry/DocNum (ou JdtNum).
    // ==========================================================
    [HttpPost("v1/{entity}")]
    public IActionResult PostEntity(string entity, [FromBody] JsonElement body)
    {
        var isJournal = entity.Equals("JournalEntries", StringComparison.OrdinalIgnoreCase);
        var docEntry = isJournal ? _store.NextJdtNum() : _store.NextDocEntry();

        _logger.LogInformation("=== POST /b1s/v1/{Entity} === DocEntry={DocEntry}", entity, docEntry);
        LogBody(body);

        // Guarda no store
        var doc = SafeParse(body);
        doc["DocEntry"] = docEntry;
        doc["DocNum"] = docEntry;
        if (isJournal)
            doc["JdtNum"] = docEntry;

        _store.Add(entity, doc);

        // Resposta
        if (isJournal)
        {
            return Created($"/b1s/v1/{entity}({docEntry})", new
            {
                JdtNum = docEntry,
                DocEntry = docEntry,
                DocNum = docEntry
            });
        }

        return Created($"/b1s/v1/{entity}({docEntry})", new
        {
            DocEntry = docEntry,
            DocNum = docEntry,
            DocTotal = 0,
            DocTotalFc = 0
        });
    }

    // ==========================================================
    // POST /b1s/v1/{entity}({id})/Cancel
    // ==========================================================
    [HttpPost("v1/{entity}({id})/Cancel")]
    public IActionResult PostCancel(string entity, int id)
    {
        _logger.LogInformation("=== CANCEL /b1s/v1/{Entity}({Id}) ===", entity, id);
        return NoContent();
    }

    // ==========================================================
    // PATCH /b1s/v1/{entity}({id})  e  /b1s/v2/{entity}({id})
    // ==========================================================
    [HttpPatch("v1/{entity}({id})")]
    [HttpPatch("v2/{entity}({id})")]
    public IActionResult PatchEntity(string entity, int id, [FromBody] JsonElement body)
    {
        _logger.LogInformation("=== PATCH /b1s/{Entity}({Id}) ===", entity, id);
        LogBody(body);
        return NoContent();
    }

    // ==========================================================
    // GET /b1s/v1/{entity}
    // Suporta $filter, $select, $orderby, $top no estilo OData.
    // Retorna {"value": [...]} como o SAP B1 real.
    // ==========================================================
    [HttpGet("v1/{entity}")]
    public IActionResult GetEntity(
        string entity,
        [FromQuery(Name = "$filter")] string? filter,
        [FromQuery(Name = "$select")] string? select,
        [FromQuery(Name = "$orderby")] string? orderby,
        [FromQuery(Name = "$top")] int? top)
    {
        _logger.LogInformation("=== GET /b1s/v1/{Entity} === $filter={Filter}", entity, filter);

        if (!_store.TryGetBag(entity, out var bag))
        {
            return Ok(new { value = Array.Empty<object>() });
        }

        var docs = bag.ToList();

        if (!string.IsNullOrEmpty(filter))
            docs = ApplyFilter(docs, filter);

        if (!string.IsNullOrEmpty(orderby))
            docs = ApplyOrderBy(docs, orderby);

        if (top.HasValue)
            docs = docs.Take(top.Value).ToList();

        if (!string.IsNullOrEmpty(select))
        {
            var fields = select.Split(',').Select(f => f.Trim()).ToHashSet();
            var filtered = docs.Select(d => FilterFields(d, fields)).ToList();
            return Ok(new { value = filtered });
        }

        return Ok(new { value = docs });
    }

    // ==========================================================
    // GET /b1s/v1/{entity}({id})
    // ==========================================================
    [HttpGet("v1/{entity}({id})")]
    public IActionResult GetEntityById(string entity, int id)
    {
        _logger.LogInformation("=== GET /b1s/v1/{Entity}({Id}) ===", entity, id);

        if (_store.TryGetBag(entity, out var bag))
        {
            var doc = bag.FirstOrDefault(d =>
                d.TryGetPropertyValue("DocEntry", out var v) && v?.GetValue<int>() == id);
            if (doc != null)
                return Ok(doc);
        }

        return NotFound(new
        {
            error = new
            {
                code = -2028,
                message = new { value = $"No matching records found for {entity}({id})" }
            }
        });
    }

    // =====================
    // Helpers
    // =====================
    private static List<JsonObject> ApplyFilter(List<JsonObject> docs, string filter)
    {
        var conditions = Regex.Matches(filter, @"(\w+)\s+eq\s+'?([^')\s]+)'?");
        if (conditions.Count == 0)
            return docs;

        bool isOr = filter.Contains(" or ", StringComparison.OrdinalIgnoreCase);

        return docs.Where(doc =>
        {
            if (isOr)
                return conditions.Cast<Match>().Any(m => MatchCondition(doc, m.Groups[1].Value, m.Groups[2].Value));
            return conditions.Cast<Match>().All(m => MatchCondition(doc, m.Groups[1].Value, m.Groups[2].Value));
        }).ToList();
    }

    private static bool MatchCondition(JsonObject doc, string field, string value)
    {
        if (!doc.TryGetPropertyValue(field, out var node) || node == null)
            return false;
        var nodeStr = node.ToJsonString().Trim('"');
        return nodeStr.Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    private static List<JsonObject> ApplyOrderBy(List<JsonObject> docs, string orderby)
    {
        var parts = orderby.Trim().Split(' ');
        var field = parts[0];
        var desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        return desc
            ? docs.OrderByDescending(d => GetSortValue(d, field)).ToList()
            : docs.OrderBy(d => GetSortValue(d, field)).ToList();
    }

    private static string GetSortValue(JsonObject doc, string field)
    {
        return doc.TryGetPropertyValue(field, out var node) && node != null
            ? node.ToJsonString().Trim('"')
            : "";
    }

    private static JsonObject FilterFields(JsonObject doc, HashSet<string> fields)
    {
        var filtered = new JsonObject();
        foreach (var field in fields)
        {
            if (doc.TryGetPropertyValue(field, out var value))
                filtered[field] = value != null ? JsonNode.Parse(value.ToJsonString()) : null;
        }
        return filtered;
    }

    private void LogBody(JsonElement body)
    {
        try { _logger.LogInformation("Payload:\n{Body}", body.GetRawText()); }
        catch { _logger.LogInformation("Payload: (não foi possível logar - encoding)"); }
    }

    private static JsonObject SafeParse(JsonElement body)
    {
        try { return JsonNode.Parse(body.GetRawText())?.AsObject() ?? new JsonObject(); }
        catch { return new JsonObject(); }
    }
}
