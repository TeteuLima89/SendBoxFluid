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
    public async Task<IActionResult> PostEntity(string entity)
    {
        var isJournal = entity.Equals("JournalEntries", StringComparison.OrdinalIgnoreCase);
        var docEntry = isJournal ? _store.NextJdtNum() : _store.NextDocEntry();

        _logger.LogInformation("=== POST /b1s/v1/{Entity} === DocEntry={DocEntry}", entity, docEntry);

        // Lê body manualmente — aceita qualquer Content-Type
        var doc = await ReadBodyAsJson();
        doc["DocEntry"] = docEntry;
        doc["DocNum"] = docEntry;
        if (isJournal)
            doc["JdtNum"] = docEntry;

        _store.Add(entity, doc);

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
    public IActionResult PatchEntity(string entity, int id)
    {
        _logger.LogInformation("=== PATCH /b1s/{Entity}({Id}) ===", entity, id);
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

        List<JsonObject> docs = new();

        if (_store.TryGetBag(entity, out var bag))
        {
            docs = bag.ToList();
            if (!string.IsNullOrEmpty(filter))
                docs = ApplyFilter(docs, filter);
        }

        // AUTO-GENERATE: se não encontrou nada, gera um documento fake
        // baseado na entidade e nos filtros da query.
        // Assim o QA não precisa fazer seed — qualquer nota funciona.
        if (docs.Count == 0 && !string.IsNullOrEmpty(filter))
        {
            var generated = AutoGenerate(entity, filter);
            if (generated != null)
            {
                _store.Add(entity, generated);
                docs = new List<JsonObject> { generated };
                _logger.LogInformation("Auto-generated {Entity} from filter: {Filter}", entity, filter);
            }
        }

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

    // ==========================================================
    // AUTO-GENERATE: gera documento fake quando GET não acha nada.
    // Extrai valores do $filter pra preencher campos-chave.
    // O QA não precisa de seed — qualquer nota funciona.
    // ==========================================================
    private JsonObject? AutoGenerate(string entity, string filter)
    {
        var filterValues = ExtractFilterValues(filter);
        var docEntry = _store.NextDocEntry();

        // Tenta pegar DocNum do filtro (campo mais comum nos GETs)
        var docNum = filterValues.GetValueOrDefault("DocNum", docEntry.ToString());

        // Base comum a todas as entidades
        var doc = new JsonObject
        {
            ["DocEntry"] = docEntry,
            ["DocNum"] = int.TryParse(docNum, out var dn) ? dn : docEntry,
            ["CardCode"] = "SANDBOX_AUTO",
            ["CardName"] = "Gerado automaticamente pelo SendBox",
            ["DocumentStatus"] = "bost_Open",
            ["CancelStatus"] = "csNo",
            ["DocCurrency"] = "BRL",
            ["DocRate"] = 1.0,
            ["DocDate"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["DocDueDate"] = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd"),
            ["BPL_IDAssignedToInvoice"] = 1,
            ["OpenForLandedCosts"] = "tYES",
        };

        // Copia todos os valores do filtro pro documento
        // ex: $filter=U_ACT_NfeId eq '123' → doc["U_ACT_NfeId"] = "123"
        foreach (var kv in filterValues)
        {
            if (!doc.ContainsKey(kv.Key))
                doc[kv.Key] = kv.Value;
        }

        // TaxExtension (usado por PurchaseOrders no fluxo de NF entrada)
        doc["TaxExtension"] = new JsonObject
        {
            ["MainUsage"] = 20,
            ["Incoterms"] = "1"
        };

        // DocumentLines (usado por quase todos os fluxos)
        doc["DocumentLines"] = new JsonArray
        {
            new JsonObject
            {
                ["LineNum"] = 0,
                ["ItemCode"] = "SANDBOX_ITEM",
                ["ItemDescription"] = "Item gerado automaticamente",
                ["Quantity"] = 1,
                ["UnitPrice"] = 100,
                ["Usage"] = 20,
                ["WarehouseCode"] = "01",
                ["Currency"] = "BRL",
                ["CFOPCode"] = "3102",
                ["Weight1"] = 1.0,
                ["DocEntry"] = docEntry
            }
        };

        // Campos específicos por entidade
        if (entity.Equals("PurchaseInvoices", StringComparison.OrdinalIgnoreCase))
        {
            doc["SequenceCode"] = -2;
            doc["SequenceSerial"] = 1;
            doc["SequenceModel"] = "M";
        }
        else if (entity.Equals("Orders", StringComparison.OrdinalIgnoreCase))
        {
            doc["NumAtCard"] = "";
            doc["Reference1"] = "";
            doc["JournalMemo"] = "Auto-generated";
            doc["PayToCode"] = "";
            doc["ShippingMethod"] = 1;
        }

        return doc;
    }

    private static Dictionary<string, string> ExtractFilterValues(string filter)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var matches = Regex.Matches(filter, @"(\w+)\s+eq\s+'?([^')\s]+)'?");
        foreach (Match m in matches)
        {
            values[m.Groups[1].Value] = m.Groups[2].Value;
        }
        return values;
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
