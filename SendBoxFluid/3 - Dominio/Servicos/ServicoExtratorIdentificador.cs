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

        // Limpa o JSON antes de parsear - Fluid envia com whitespace
        // e as vezes string-encoded (envolto em aspas com \r\n escapado).
        var jsonLimpo = LimparJson(corpoRequisicao);
        if (string.IsNullOrWhiteSpace(jsonLimpo))
            return null;

        try
        {
            using var documento = JsonDocument.Parse(jsonLimpo);
            var raiz = documento.RootElement;

            // Se ainda assim virou string, tenta parsear o conteudo
            if (raiz.ValueKind == JsonValueKind.String)
            {
                var conteudoString = raiz.GetString();
                if (string.IsNullOrWhiteSpace(conteudoString)) return null;
                try
                {
                    using var doc2 = JsonDocument.Parse(LimparJson(conteudoString));
                    return ExtrairDoElemento(doc2.RootElement);
                }
                catch { return null; }
            }

            return ExtrairDoElemento(raiz);
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtrairDoElemento(JsonElement raiz)
    {
        // Tenta campos SAP B1
        foreach (var campo in CamposIdentificadorSap)
        {
            var valor = ObterValorCampo(raiz, campo);
            if (!string.IsNullOrEmpty(valor)) return valor;
        }

        // Tenta campos Sankhya
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

    /// <summary>
    /// Mesma logica do ServicoFormatadorJson - desempacota string-encoded
    /// JSON, remove escapes literais, acha o inicio do JSON real.
    /// </summary>
    private static string LimparJson(string entrada)
    {
        var json = entrada.Trim();

        if (json.StartsWith("\"") && json.EndsWith("\""))
        {
            try
            {
                var desempacotado = JsonSerializer.Deserialize<string>(json);
                if (!string.IsNullOrWhiteSpace(desempacotado))
                    json = desempacotado.Trim();
            }
            catch { }
        }

        if (json.Contains("\\r\\n") || json.Contains("\\n"))
        {
            json = json.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\\"", "\"");
        }

        var indiceObjeto = json.IndexOf('{');
        var indiceArray = json.IndexOf('[');
        var indiceInicio = -1;

        if (indiceObjeto >= 0 && indiceArray >= 0)
            indiceInicio = Math.Min(indiceObjeto, indiceArray);
        else if (indiceObjeto >= 0)
            indiceInicio = indiceObjeto;
        else if (indiceArray >= 0)
            indiceInicio = indiceArray;

        if (indiceInicio > 0)
            json = json[indiceInicio..];

        return json;
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
