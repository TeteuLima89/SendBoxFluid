namespace SendBoxFluid.Dominio.Servicos;

/// <summary>
/// Converte data/hora para o fuso de Brasilia (UTC-3).
/// Render hospeda em UTC, entao precisamos converter pra exibir corretamente.
/// </summary>
public static class ServicoFusoHorario
{
    private static readonly TimeZoneInfo FusoBrasilia = ObterFusoBrasilia();

    public static DateTime AgoraBrasilia()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoBrasilia);

    public static DateTime ParaBrasilia(DateTime data)
    {
        if (data.Kind == DateTimeKind.Utc)
            return TimeZoneInfo.ConvertTimeFromUtc(data, FusoBrasilia);
        if (data.Kind == DateTimeKind.Local)
            return TimeZoneInfo.ConvertTimeFromUtc(data.ToUniversalTime(), FusoBrasilia);
        // Unspecified - assume que veio do banco/sistema como UTC
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(data, DateTimeKind.Utc), FusoBrasilia);
    }

    private static TimeZoneInfo ObterFusoBrasilia()
    {
        // Tenta ID Linux (Render eh Linux) e Windows
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch { }
        try { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
        catch { }
        // Fallback: cria manualmente UTC-3
        return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasilia", "Brasilia");
    }
}
