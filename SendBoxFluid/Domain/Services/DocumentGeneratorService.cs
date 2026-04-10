using System.Text.Json.Nodes;
using SendBoxFluid.Domain.Interfaces;

namespace SendBoxFluid.Domain.Services;

/// <summary>
/// Gera documentos SAP B1 fake automaticamente quando o GET não encontra
/// nada no store. Extrai valores do $filter pra preencher campos-chave.
/// O QA não precisa de seed — qualquer nota funciona.
/// </summary>
public class DocumentGeneratorService
{
    private readonly IDocumentRepository _repository;

    public DocumentGeneratorService(IDocumentRepository repository)
    {
        _repository = repository;
    }

    public JsonObject Generate(string entity, string filter)
    {
        var filterValues = ODataFilterService.ExtractFilterValues(filter);
        var docEntry = _repository.NextDocEntry();
        var docNum = filterValues.GetValueOrDefault("DocNum", docEntry.ToString());

        var doc = BuildBaseDocument(docEntry, docNum, filterValues);
        AddTaxExtension(doc);
        AddDocumentLines(doc, docEntry);
        AddEntitySpecificFields(doc, entity);

        return doc;
    }

    private static JsonObject BuildBaseDocument(int docEntry, string docNum, Dictionary<string, string> filterValues)
    {
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

        // Copia valores do filtro pro documento
        // ex: $filter=U_ACT_NfeId eq '123' → doc["U_ACT_NfeId"] = "123"
        foreach (var kv in filterValues)
        {
            if (!doc.ContainsKey(kv.Key))
                doc[kv.Key] = kv.Value;
        }

        return doc;
    }

    private static void AddTaxExtension(JsonObject doc)
    {
        doc["TaxExtension"] = new JsonObject
        {
            ["MainUsage"] = 20,
            ["Incoterms"] = "1"
        };
    }

    private static void AddDocumentLines(JsonObject doc, int docEntry)
    {
        var lines = new JsonArray();
        for (int i = 0; i < 1000; i++)
        {
            lines.Add(new JsonObject
            {
                ["LineNum"] = i,
                ["ItemCode"] = $"SANDBOX_ITEM_{i}",
                ["ItemDescription"] = $"Item linha {i}",
                ["Quantity"] = 1,
                ["UnitPrice"] = 100,
                ["Usage"] = 20,
                ["WarehouseCode"] = "01",
                ["Currency"] = "BRL",
                ["CFOPCode"] = "3102",
                ["Weight1"] = 1.0,
                ["DocEntry"] = docEntry
            });
        }
        doc["DocumentLines"] = lines;
    }

    private static void AddEntitySpecificFields(JsonObject doc, string entity)
    {
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
    }
}
