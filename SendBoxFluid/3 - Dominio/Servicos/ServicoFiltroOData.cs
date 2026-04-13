using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SendBoxFluid.Dominio.Servicos;

/// <summary>
/// Aplica filtros OData simples ($filter, $select, $orderby, $top)
/// sobre listas de JsonObject, simulando o comportamento do SAP B1.
/// </summary>
public static class ServicoFiltroOData
{
    public static List<JsonObject> AplicarFiltro(List<JsonObject> documentos, string filtro)
    {
        var condicoes = Regex.Matches(filtro, @"(\w+)\s+eq\s+'?([^')\s]+)'?");
        if (condicoes.Count == 0)
            return documentos;

        bool ehOuLogico = filtro.Contains(" or ", StringComparison.OrdinalIgnoreCase);

        return documentos.Where(documento =>
        {
            if (ehOuLogico)
                return condicoes.Cast<Match>().Any(m => CombinaCondicao(documento, m.Groups[1].Value, m.Groups[2].Value));
            return condicoes.Cast<Match>().All(m => CombinaCondicao(documento, m.Groups[1].Value, m.Groups[2].Value));
        }).ToList();
    }

    public static List<JsonObject> AplicarOrdenacao(List<JsonObject> documentos, string ordenacao)
    {
        var partes = ordenacao.Trim().Split(' ');
        var campo = partes[0];
        var descendente = partes.Length > 1 && partes[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        return descendente
            ? documentos.OrderByDescending(d => ObterValorOrdenacao(d, campo)).ToList()
            : documentos.OrderBy(d => ObterValorOrdenacao(d, campo)).ToList();
    }

    public static JsonObject FiltrarCampos(JsonObject documento, HashSet<string> campos)
    {
        var filtrado = new JsonObject();
        foreach (var campo in campos)
        {
            if (documento.TryGetPropertyValue(campo, out var valor))
                filtrado[campo] = valor != null ? JsonNode.Parse(valor.ToJsonString()) : null;
        }
        return filtrado;
    }

    public static Dictionary<string, string> ExtrairValoresFiltro(string filtro)
    {
        var valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var coincidencias = Regex.Matches(filtro, @"(\w+)\s+eq\s+'?([^')\s]+)'?");
        foreach (Match m in coincidencias)
        {
            valores[m.Groups[1].Value] = m.Groups[2].Value;
        }
        return valores;
    }

    private static bool CombinaCondicao(JsonObject documento, string campo, string valor)
    {
        if (!documento.TryGetPropertyValue(campo, out var no) || no == null)
            return false;
        var textoNo = no.ToJsonString().Trim('"');
        return textoNo.Equals(valor, StringComparison.OrdinalIgnoreCase);
    }

    private static string ObterValorOrdenacao(JsonObject documento, string campo)
    {
        return documento.TryGetPropertyValue(campo, out var no) && no != null
            ? no.ToJsonString().Trim('"')
            : "";
    }
}
