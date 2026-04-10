using System.Text.Json.Nodes;

namespace SendBoxFluid.Domain.Entities;

/// <summary>
/// Representa um documento SAP B1 genérico (PurchaseOrder, Draft, Invoice, etc).
/// Wrapper sobre JsonObject que garante campos obrigatórios.
/// </summary>
public class SapDocument
{
    public JsonObject Data { get; }

    public int DocEntry => Data["DocEntry"]?.GetValue<int>() ?? 0;
    public string Entity { get; }

    public SapDocument(string entity, JsonObject data)
    {
        Entity = entity;
        Data = data;
    }
}
