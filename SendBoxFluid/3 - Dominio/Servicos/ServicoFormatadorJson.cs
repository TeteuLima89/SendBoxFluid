using System.Text.Json;

namespace SendBoxFluid.Dominio.Servicos;

/// <summary>
/// Formata strings JSON para exibicao no painel.
/// Lida com casos onde o Fluid manda JSON wrapped em string,
/// com whitespace excessivo, ou com escape duplo.
/// </summary>
public static class ServicoFormatadorJson
{
    private static readonly JsonSerializerOptions OpcoesSerializacaoIdentada = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Formatar(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
            return string.Empty;

        // Remove whitespace excessivo nas extremidades
        var json = entrada.Trim();

        // Caso 1: vem envolto em aspas (string-encoded JSON)
        // Ex: "\"\\n\\n{\\\"campo\\\":...}\""
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

        // Caso 2: tem caracteres de escape literal (\r\n no texto)
        // Substitui por quebras reais
        if (json.Contains("\\r\\n") || json.Contains("\\n"))
        {
            json = json.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\\"", "\"");
        }

        // Tenta achar onde comeca o JSON real (primeiro { ou [)
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

        // Tenta parsear e formatar bonitinho
        try
        {
            using var documento = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(documento, OpcoesSerializacaoIdentada);
        }
        catch
        {
            // Se nao for JSON valido, retorna texto limpo (sem escapes)
            return json;
        }
    }
}
