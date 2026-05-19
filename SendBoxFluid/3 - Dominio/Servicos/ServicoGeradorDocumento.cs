using System.Text.Json.Nodes;
using SendBoxFluid.Dominio.Interfaces.Repositorios;

namespace SendBoxFluid.Dominio.Servicos;

/// <summary>
/// Gera documentos SAP B1 fake automaticamente quando o GET nao acha nada.
/// Extrai valores do $filter pra preencher campos-chave.
/// O QA nao precisa fazer seed - qualquer nota funciona.
/// </summary>
public class ServicoGeradorDocumento
{
    private const int QuantidadeLinhasGeradas = 1000;
    private readonly IRepositorioDocumento _repositorioDocumento;

    public ServicoGeradorDocumento(IRepositorioDocumento repositorioDocumento)
    {
        _repositorioDocumento = repositorioDocumento;
    }

    public JsonObject Gerar(string entidade, string filtro)
    {
        var valoresFiltro = ServicoFiltroOData.ExtrairValoresFiltro(filtro);
        var codigoEntrada = _repositorioDocumento.ProximoCodigoEntrada();
        var numeroDocumento = valoresFiltro.GetValueOrDefault("DocNum", codigoEntrada.ToString());

        var documento = MontarDocumentoBase(codigoEntrada, numeroDocumento, valoresFiltro);
        AdicionarExtensaoFiscal(documento);
        AdicionarLinhasDocumento(documento, codigoEntrada);
        AdicionarCamposEspecificosEntidade(documento, entidade);

        return documento;
    }

    private static JsonObject MontarDocumentoBase(int codigoEntrada, string numeroDocumento, Dictionary<string, string> valoresFiltro)
    {
        var documento = new JsonObject
        {
            ["DocEntry"] = codigoEntrada,
            ["DocNum"] = int.TryParse(numeroDocumento, out var dn) ? dn : codigoEntrada,
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

        // Copia valores do filtro pro documento (ex: U_ACT_NfeId eq '123')
        foreach (var kv in valoresFiltro)
        {
            if (!documento.ContainsKey(kv.Key))
                documento[kv.Key] = kv.Value;
        }

        return documento;
    }

    private static void AdicionarExtensaoFiscal(JsonObject documento)
    {
        documento["TaxExtension"] = new JsonObject
        {
            ["MainUsage"] = 20,
            ["Incoterms"] = "1"
        };
    }

    private static void AdicionarLinhasDocumento(JsonObject documento, int codigoEntrada)
    {
        var linhas = new JsonArray();
        for (int i = 0; i < QuantidadeLinhasGeradas; i++)
        {
            linhas.Add(new JsonObject
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
                ["DocEntry"] = codigoEntrada
            });
        }
        documento["DocumentLines"] = linhas;
    }

    private static void AdicionarCamposEspecificosEntidade(JsonObject documento, string entidade)
    {
        if (entidade.Equals("PurchaseInvoices", StringComparison.OrdinalIgnoreCase) ||
            entidade.Equals("Invoices", StringComparison.OrdinalIgnoreCase))
        {
            documento["SequenceCode"] = -2;
            documento["SequenceSerial"] = 1;
            documento["SequenceModel"] = "M";
        }
        else if (entidade.Equals("Orders", StringComparison.OrdinalIgnoreCase))
        {
            documento["NumAtCard"] = "";
            documento["Reference1"] = "";
            documento["JournalMemo"] = "Auto-generated";
            documento["PayToCode"] = "";
            documento["ShippingMethod"] = 1;
        }
        else if (entidade.Equals("PurchaseOrders", StringComparison.OrdinalIgnoreCase))
        {
            documento["NumAtCard"] = "";
            documento["CardCode"] = "SANDBOX_AUTO";
            documento["PaymentGroupCode"] = -1;
            documento["DocObjectCode"] = "22";
        }
        else if (entidade.Equals("StockTransfers", StringComparison.OrdinalIgnoreCase))
        {
            documento["FromWarehouse"] = "01";
            documento["ToWarehouse"] = "02";
            documento["DocObjectCode"] = "67";
            var linhasJson = documento["DocumentLines"]?.ToJsonString();
            if (linhasJson != null)
                documento["StockTransferLines"] = System.Text.Json.Nodes.JsonNode.Parse(linhasJson);
        }
    }
}
