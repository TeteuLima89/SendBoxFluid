using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SendBoxFluid.Domain.Services;

/// <summary>
/// Aplica filtros OData simples ($filter, $select, $orderby, $top)
/// sobre listas de JsonObject — simula o comportamento do SAP B1.
/// </summary>
public static class ODataFilterService
{
    public static List<JsonObject> ApplyFilter(List<JsonObject> docs, string filter)
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

    public static List<JsonObject> ApplyOrderBy(List<JsonObject> docs, string orderby)
    {
        var parts = orderby.Trim().Split(' ');
        var field = parts[0];
        var desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        return desc
            ? docs.OrderByDescending(d => GetSortValue(d, field)).ToList()
            : docs.OrderBy(d => GetSortValue(d, field)).ToList();
    }

    public static JsonObject FilterFields(JsonObject doc, HashSet<string> fields)
    {
        var filtered = new JsonObject();
        foreach (var field in fields)
        {
            if (doc.TryGetPropertyValue(field, out var value))
                filtered[field] = value != null ? JsonNode.Parse(value.ToJsonString()) : null;
        }
        return filtered;
    }

    public static Dictionary<string, string> ExtractFilterValues(string filter)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var matches = Regex.Matches(filter, @"(\w+)\s+eq\s+'?([^')\s]+)'?");
        foreach (Match m in matches)
        {
            values[m.Groups[1].Value] = m.Groups[2].Value;
        }
        return values;
    }

    private static bool MatchCondition(JsonObject doc, string field, string value)
    {
        if (!doc.TryGetPropertyValue(field, out var node) || node == null)
            return false;
        var nodeStr = node.ToJsonString().Trim('"');
        return nodeStr.Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSortValue(JsonObject doc, string field)
    {
        return doc.TryGetPropertyValue(field, out var node) && node != null
            ? node.ToJsonString().Trim('"')
            : "";
    }
}
