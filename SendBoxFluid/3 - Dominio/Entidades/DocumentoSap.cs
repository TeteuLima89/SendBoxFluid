using System.Text.Json.Nodes;

namespace SendBoxFluid.Dominio.Entidades;

/// <summary>
/// Representa um documento SAP B1 generico (PurchaseOrder, Draft, Invoice, etc).
/// Armazenado no repositorio em memoria do SendBox.
/// </summary>
public class DocumentoSap
{
    public string Entidade { get; }
    public JsonObject Dados { get; }
    public int CodigoEntrada => Dados["DocEntry"]?.GetValue<int>() ?? 0;
    public int NumeroDocumento => Dados["DocNum"]?.GetValue<int>() ?? 0;

    public DocumentoSap(string entidade, JsonObject dados)
    {
        Entidade = entidade;
        Dados = dados;
    }
}
