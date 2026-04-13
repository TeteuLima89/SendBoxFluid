using System.Text.Json;

namespace SendBoxFluid.Dominio.Servicos;

/// <summary>
/// Extrai identificadores de negocio dos corpos das requisicoes
/// (NfeId, ProcessoId, PedidoId, etc) pra mostrar na listagem do painel.
/// Facilita o QA achar a integracao especifica.
/// </summary>
public static class ServicoExtratorIdentificador
{
    /// <summary>
    /// Campos comumente usados como identificador nos POSTs do SAP B1.
    /// Tenta na ordem - retorna o primeiro que achar.
    /// </summary>
    private static readonly string[] CamposIdentificadorSap = new[]
    {
        "U_ACT_NfeId",       // Nota fiscal entrada
        "U_ACT_ComexId",     // Processo de importacao
        "U_ACT_Invoice",     // Invoice
        "U_ChaveAcesso",     // NFe chave de acesso
        "Reference2",        // Referencia secundaria
        "Reference1",        // Referencia
        "NumAtCard",         // Numero no cartao
        "VendorCode",        // Fornecedor (LandedCost)
        "CardCode"           // Codigo de parceiro (fallback)
    };

    /// <summary>
    /// Campos do Sankhya.
    /// </summary>
    private static readonly string[] CamposIdentificadorSankhya = new[]
    {
        "NUNOTA",
        "NUFIN",
        "NUMNOTA",
        "AD_NUMNFI",
        "CHAVENFE"
    };

    public static string? Extrair(string corpoRequisicao)
    {
        if (string.IsNullOrWhiteSpace(corpoRequisicao))
            return null;

        try
        {
            using var documento = JsonDocument.Parse(corpoRequisicao);
            var raiz = documento.RootElement;

            // Tenta campos SAP B1
            foreach (var campo in CamposIdentificadorSap)
            {
                var valor = ObterValorCampo(raiz, campo);
                if (!string.IsNullOrEmpty(valor)) return valor;
            }

            // Tenta campos Sankhya (geralmente em dataSet/rootEntity)
            var dataSet = ObterPropriedade(raiz, "serviceName") != null
                ? ObterPropriedadeRecursiva(raiz, "PK") ?? ObterPropriedadeRecursiva(raiz, "values")
                : raiz;

            if (dataSet.HasValue)
            {
                foreach (var campo in CamposIdentificadorSankhya)
                {
                    var valor = ObterValorCampo(dataSet.Value, campo);
                    if (!string.IsNullOrEmpty(valor)) return valor;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ObterValorCampo(JsonElement elemento, string nomeCampo)
    {
        if (elemento.ValueKind != JsonValueKind.Object) return null;

        if (!elemento.TryGetProperty(nomeCampo, out var valor)) return null;

        return valor.ValueKind switch
        {
            JsonValueKind.String => valor.GetString(),
            JsonValueKind.Number => valor.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => valor.ToString()
        };
    }

    private static JsonElement? ObterPropriedade(JsonElement elemento, string nome)
    {
        if (elemento.ValueKind == JsonValueKind.Object && elemento.TryGetProperty(nome, out var v))
            return v;
        return null;
    }

    private static JsonElement? ObterPropriedadeRecursiva(JsonElement elemento, string nome)
    {
        if (elemento.ValueKind == JsonValueKind.Object)
        {
            if (elemento.TryGetProperty(nome, out var direto))
                return direto;
            foreach (var prop in elemento.EnumerateObject())
            {
                var resultado = ObterPropriedadeRecursiva(prop.Value, nome);
                if (resultado.HasValue) return resultado;
            }
        }
        return null;
    }
}
