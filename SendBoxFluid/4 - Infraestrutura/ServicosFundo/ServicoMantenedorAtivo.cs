namespace SendBoxFluid.Infraestrutura.ServicosFundo;

/// <summary>
/// Mantem o servico ativo no Render free tier fazendo auto-ping a cada 10min.
/// O Render coloca em sleep apos 15min sem requisicao - quando isso acontece,
/// a instancia eh destruida e a memoria perdida.
/// Com auto-ping, o servico nunca dorme e os dados em memoria persistem.
/// </summary>
public class ServicoMantenedorAtivo : BackgroundService
{
    private readonly ILogger<ServicoMantenedorAtivo> _registradorEventos;
    private readonly IConfiguration _configuracao;
    private static readonly HttpClient _clienteHttp = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly TimeSpan IntervaloAutoPing = TimeSpan.FromMinutes(10);

    public ServicoMantenedorAtivo(
        ILogger<ServicoMantenedorAtivo> registradorEventos,
        IConfiguration configuracao)
    {
        _registradorEventos = registradorEventos;
        _configuracao = configuracao;
    }

    protected override async Task ExecuteAsync(CancellationToken tokenCancelamento)
    {
        // Aguarda o app subir antes do primeiro ping
        await Task.Delay(TimeSpan.FromSeconds(30), tokenCancelamento);

        var urlPublica = ObterUrlPublica();
        if (string.IsNullOrEmpty(urlPublica))
        {
            _registradorEventos.LogInformation("Auto-ping desativado (sem URL publica configurada)");
            return;
        }

        _registradorEventos.LogInformation("Auto-ping iniciado: {Url} a cada {Min}min",
            urlPublica, IntervaloAutoPing.TotalMinutes);

        while (!tokenCancelamento.IsCancellationRequested)
        {
            try
            {
                var resposta = await _clienteHttp.GetAsync($"{urlPublica}/saude", tokenCancelamento);
                _registradorEventos.LogInformation("Auto-ping: HTTP {Status}", (int)resposta.StatusCode);
            }
            catch (Exception ex)
            {
                _registradorEventos.LogWarning("Falha no auto-ping: {Mensagem}", ex.Message);
            }

            await Task.Delay(IntervaloAutoPing, tokenCancelamento);
        }
    }

    private string? ObterUrlPublica()
    {
        // Tenta variavel de ambiente (config no Render)
        var url = Environment.GetEnvironmentVariable("URL_PUBLICA");
        if (!string.IsNullOrEmpty(url)) return url.TrimEnd('/');

        // Tenta config padrao
        url = _configuracao["UrlPublica"];
        if (!string.IsNullOrEmpty(url)) return url.TrimEnd('/');

        // Fallback hardcoded para o Render
        if (Environment.GetEnvironmentVariable("RENDER") != null)
            return "https://sendboxfluid.onrender.com";

        return null;
    }
}
